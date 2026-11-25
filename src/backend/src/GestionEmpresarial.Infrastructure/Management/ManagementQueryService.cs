using GestionEmpresarial.Application.Management;
using GestionEmpresarial.Domain.Entities;
using GestionEmpresarial.Domain.Enums;
using GestionEmpresarial.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionEmpresarial.Infrastructure.Management;

public sealed class ManagementQueryService(
    ApplicationDbContext dbContext,
    UserManager<IdentityUser> userManager) : IManagementQueryService
{
    public async Task<DashboardSummaryDto> GetDashboardAsync(
        PeriodQuery query,
        string currentUserId,
        bool canViewSales,
        bool canViewAllSales,
        bool canViewInventory,
        CancellationToken cancellationToken)
    {
        var period = GetPeriod(query);
        var sales = SalesInPeriod(period.FromUtc, period.ThroughUtc);
        if (canViewSales && !canViewAllSales)
        {
            sales = sales.Where(sale => sale.CreatedByUserId == currentUserId);
        }

        var saleCount = canViewSales ? await sales.CountAsync(cancellationToken) : (int?)null;
        decimal? salesTotal = canViewSales
            ? await sales.Select(sale => (decimal?)sale.Total).SumAsync(cancellationToken) ?? 0m
            : null;
        var topProducts = canViewSales
            ? await GetTopProductsAsync(period.FromUtc, period.ThroughUtc, currentUserId, canViewAllSales, cancellationToken)
            : [];

        var lowStock = canViewInventory
            ? dbContext.Products.AsNoTracking().Where(product => product.IsActive && product.CurrentStock <= product.MinimumStock)
            : dbContext.Products.AsNoTracking().Where(_ => false);
        var lowStockCount = canViewInventory ? await lowStock.CountAsync(cancellationToken) : (int?)null;
        var lowStockProducts = canViewInventory
            ? await lowStock
                .OrderBy(product => product.CurrentStock)
                .ThenBy(product => product.Name)
                .Take(5)
                .Select(product => new DashboardLowStockDto(
                    product.ProductId,
                    product.SKU,
                    product.Name,
                    product.CurrentStock,
                    product.MinimumStock))
                .ToListAsync(cancellationToken)
            : [];

        return new DashboardSummaryDto(
            period.FromDate,
            period.ToDate,
            salesTotal,
            saleCount,
            lowStockCount,
            lowStockProducts,
            topProducts);
    }

    public async Task<ReportsSummaryDto> GetReportsAsync(PeriodQuery query, CancellationToken cancellationToken)
    {
        var period = GetPeriod(query);
        var sales = SalesInPeriod(period.FromUtc, period.ThroughUtc);
        List<SalesByDayDto> salesByDay;
        if (dbContext.Database.IsRelational())
        {
            salesByDay = await sales
                .GroupBy(sale => sale.CreatedAtUtc.Date)
                .Select(group => new SalesByDayDto(
                    group.Key,
                    group.Count(),
                    group.Sum(sale => sale.Total)))
                .OrderBy(item => item.DayUtc)
                .ToListAsync(cancellationToken);
        }
        else
        {
            var dayValues = await sales
                .Select(sale => new { DayUtc = sale.CreatedAtUtc.Date, sale.Total })
                .ToListAsync(cancellationToken);
            salesByDay = dayValues
                .GroupBy(item => item.DayUtc)
                .Select(group => new SalesByDayDto(group.Key, group.Count(), group.Sum(item => item.Total)))
                .OrderBy(item => item.DayUtc)
                .ToList();
        }
        var topProducts = await GetTopProductsAsync(
            period.FromUtc,
            period.ThroughUtc,
            currentUserId: string.Empty,
            canViewAllSales: true,
            cancellationToken);

        var movementQuery = dbContext.StockMovements
            .AsNoTracking()
            .Where(movement => movement.CreatedAtUtc >= period.FromUtc && movement.CreatedAtUtc <= period.ThroughUtc);
        List<MovementSummaryDto> movements;
        if (dbContext.Database.IsRelational())
        {
            movements = await movementQuery
                .GroupBy(movement => movement.MovementType)
                .Select(group => new MovementSummaryDto(
                    group.Key,
                    group.Count(),
                    group.Sum(movement => movement.QuantityDelta)))
                .OrderBy(item => item.Type)
                .ToListAsync(cancellationToken);
        }
        else
        {
            var movementRows = await movementQuery
                .Select(movement => new { movement.MovementType, movement.QuantityDelta })
                .ToListAsync(cancellationToken);
            movements = movementRows
                .GroupBy(item => item.MovementType)
                .Select(group => new MovementSummaryDto(group.Key, group.Count(), group.Sum(item => item.QuantityDelta)))
                .OrderBy(item => item.Type)
                .ToList();
        }

        return new ReportsSummaryDto(period.FromDate, period.ToDate, salesByDay, topProducts, movements);
    }

    public async Task<ManagementPageResult<AuditLogDto>> GetAuditLogsAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken)
    {
        var period = GetPeriod(query.FromDate, query.ToDate);
        var logs =
            from log in dbContext.AuditLogs.AsNoTracking()
            join user in userManager.Users.AsNoTracking() on log.UserId equals user.Id into actors
            from actor in actors.DefaultIfEmpty()
            where log.CreatedAtUtc >= period.FromUtc && log.CreatedAtUtc <= period.ThroughUtc
            select new { Log = log, UserName = actor == null ? log.UserId : actor.UserName ?? log.UserId };

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            logs = logs.Where(item => item.Log.UserId == query.UserId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            logs = logs.Where(item => item.Log.Action.Contains(search)
                || item.Log.EntityName.Contains(search)
                || (item.Log.EntityId != null && item.Log.EntityId.Contains(search))
                || item.UserName.Contains(search));
        }

        var totalCount = await logs.CountAsync(cancellationToken);
        var skip = (int)Math.Min((long)(query.Page - 1) * query.PageSize, int.MaxValue);
        var items = await logs
            .OrderByDescending(item => item.Log.CreatedAtUtc)
            .ThenByDescending(item => item.Log.AuditLogId)
            .Skip(skip)
            .Take(query.PageSize)
            .Select(item => new AuditLogDto(
                item.Log.AuditLogId,
                item.Log.UserId,
                item.UserName,
                item.Log.Action,
                item.Log.EntityName,
                item.Log.EntityId,
                item.Log.DetailsJson,
                item.Log.IpAddress,
                item.Log.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return CreatePage(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<ManagementPageResult<UserDirectoryDto>> GetUsersAsync(
        UserDirectoryQuery query,
        CancellationToken cancellationToken)
    {
        var users = userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            users = users.Where(user => user.UserName!.Contains(search)
                || (user.Email != null && user.Email.Contains(search)));
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var skip = (int)Math.Min((long)(query.Page - 1) * query.PageSize, int.MaxValue);
        var pageUsers = await users
            .OrderBy(user => user.UserName)
            .ThenBy(user => user.Id)
            .Skip(skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var items = new List<UserDirectoryDto>(pageUsers.Count);
        foreach (var user in pageUsers)
        {
            items.Add(new UserDirectoryDto(
                user.Id,
                user.UserName ?? string.Empty,
                user.Email,
                (await userManager.GetRolesAsync(user)).ToArray(),
                await userManager.IsLockedOutAsync(user)));
        }

        return CreatePage(items, query.Page, query.PageSize, totalCount);
    }

    private IQueryable<Sale> SalesInPeriod(DateTime fromUtc, DateTime throughUtc) =>
        dbContext.Sales.AsNoTracking()
            .Where(sale => sale.Status == SaleStatus.Completed
                && sale.CreatedAtUtc >= fromUtc
                && sale.CreatedAtUtc <= throughUtc);

    private async Task<List<TopSellingProductDto>> GetTopProductsAsync(
        DateTime fromUtc,
        DateTime throughUtc,
        string currentUserId,
        bool canViewAllSales,
        CancellationToken cancellationToken)
    {
        var sales = SalesInPeriod(fromUtc, throughUtc);
        if (!canViewAllSales)
        {
            sales = sales.Where(sale => sale.CreatedByUserId == currentUserId);
        }

        var saleIds = sales.Select(sale => sale.SaleId);
        if (!dbContext.Database.IsRelational())
        {
            var selectedSaleIds = await saleIds.ToArrayAsync(cancellationToken);
            var rows = await dbContext.SaleItems.AsNoTracking()
                .Where(item => selectedSaleIds.Contains(item.SaleId))
                .Select(item => new { item.ProductId, item.SKU, item.ProductName, item.Quantity, item.LineTotal })
                .ToListAsync(cancellationToken);
            return rows
                .GroupBy(item => new { item.ProductId, item.SKU, item.ProductName })
                .Select(group => new TopSellingProductDto(
                    group.Key.ProductId,
                    group.Key.SKU,
                    group.Key.ProductName,
                    group.Sum(item => item.Quantity),
                    group.Sum(item => item.LineTotal)))
                .OrderByDescending(item => item.Quantity)
                .ThenBy(item => item.Name)
                .Take(5)
                .ToList();
        }

        return await dbContext.SaleItems.AsNoTracking()
            .Where(item => saleIds.Contains(item.SaleId))
            .GroupBy(item => new { item.ProductId, item.SKU, item.ProductName })
            .Select(group => new TopSellingProductDto(
                group.Key.ProductId,
                group.Key.SKU,
                group.Key.ProductName,
                group.Sum(item => item.Quantity),
                group.Sum(item => item.LineTotal)))
            .OrderByDescending(item => item.Quantity)
            .ThenBy(item => item.Name)
            .Take(5)
            .ToListAsync(cancellationToken);
    }

    private static (DateOnly FromDate, DateOnly ToDate, DateTime FromUtc, DateTime ThroughUtc) GetPeriod(PeriodQuery query) =>
        GetPeriod(query.FromDate, query.ToDate);

    private static (DateOnly FromDate, DateOnly ToDate, DateTime FromUtc, DateTime ThroughUtc) GetPeriod(
        DateOnly? fromDate,
        DateOnly? toDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = fromDate ?? new DateOnly(today.Year, today.Month, 1);
        var end = toDate ?? today;
        if (start > end)
        {
            throw ManagementQueryException.InvalidPeriod();
        }

        var fromUtc = DateTime.SpecifyKind(start.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var throughUtc = DateTime.SpecifyKind(end.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
        return (start, end, fromUtc, throughUtc);
    }

    private static ManagementPageResult<T> CreatePage<T>(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new ManagementPageResult<T>(items, page, pageSize, totalCount, totalPages);
    }
}
