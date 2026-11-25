using System.Data;
using System.Text.Json;
using GestionEmpresarial.Application.Sales;
using GestionEmpresarial.Domain.Entities;
using GestionEmpresarial.Domain.Enums;
using GestionEmpresarial.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GestionEmpresarial.Infrastructure.Sales;

public sealed class SaleService(ApplicationDbContext dbContext) : ISaleService
{
    private const decimal MaximumMoneyValue = 9999999999999999.99m;
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SalePageResult<SaleSummaryDto>> GetSalesAsync(
        SaleQuery query,
        string currentUserId,
        bool canViewAll,
        CancellationToken cancellationToken)
    {
        if (query.FromDate.HasValue && query.ToDate.HasValue && query.FromDate > query.ToDate)
        {
            throw SaleServiceException.BadRequest("invalid_sale_date_range", "La fecha inicial no puede ser posterior a la fecha final.");
        }

        var sales = dbContext.Sales.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            sales = sales.Where(sale => sale.SaleNumber.Contains(search));
        }

        if (query.FromDate.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(query.FromDate.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            sales = sales.Where(sale => sale.CreatedAtUtc >= fromUtc);
        }

        if (query.ToDate.HasValue)
        {
            var throughUtc = DateTime.SpecifyKind(query.ToDate.Value.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
            sales = sales.Where(sale => sale.CreatedAtUtc <= throughUtc);
        }

        var creatorId = canViewAll ? query.CreatedByUserId : currentUserId;
        if (!string.IsNullOrWhiteSpace(creatorId))
        {
            sales = sales.Where(sale => sale.CreatedByUserId == creatorId);
        }

        var totalCount = await sales.CountAsync(cancellationToken);
        var skip = (int)Math.Min((long)(query.Page - 1) * query.PageSize, int.MaxValue);
        var items = await sales
            .OrderByDescending(sale => sale.CreatedAtUtc)
            .ThenByDescending(sale => sale.SaleId)
            .Skip(skip)
            .Take(query.PageSize)
            .Select(sale => new SaleSummaryDto(
                sale.SaleId,
                sale.SaleNumber,
                sale.Status,
                sale.Subtotal,
                sale.Discount,
                sale.Total,
                sale.PaymentMethod,
                sale.Items.Count,
                sale.CreatedByUserId,
                sale.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new SalePageResult<SaleSummaryDto>(items, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<SaleDetailDto?> GetSaleAsync(
        long saleId,
        string currentUserId,
        bool canViewAll,
        CancellationToken cancellationToken)
    {
        var sale = await dbContext.Sales
            .AsNoTracking()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.SaleId == saleId
                && (canViewAll || item.CreatedByUserId == currentUserId), cancellationToken);

        return sale is null ? null : ToDetailDto(sale);
    }

    public async Task<SaleDetailDto> CreateSaleAsync(
        SaleWriteRequest request,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count is < 1 or > 100)
        {
            throw SaleServiceException.BadRequest("sale_items_required", "La venta debe incluir entre 1 y 100 lineas.");
        }

        if (request.Items.Any(item => item is null))
        {
            throw SaleServiceException.BadRequest("invalid_sale_line", "La venta no puede contener lineas vacias.");
        }

        if (!Enum.IsDefined(request.PaymentMethod))
        {
            throw SaleServiceException.BadRequest("invalid_payment_method", "Selecciona un metodo de pago valido.");
        }

        if (request.Items.Any(item => item.ProductId <= 0 || item.Quantity <= 0))
        {
            throw SaleServiceException.BadRequest("invalid_sale_line", "Cada linea requiere un producto valido y una cantidad positiva.");
        }

        var productIds = request.Items.Select(item => item.ProductId).ToArray();
        if (productIds.Distinct().Count() != productIds.Length)
        {
            throw SaleServiceException.BadRequest("duplicate_sale_product", "Cada producto debe aparecer una sola vez en el carrito.");
        }

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
            : null;

        var products = await dbContext.Products
            .Where(item => productIds.Contains(item.ProductId))
            .ToDictionaryAsync(item => item.ProductId, cancellationToken);
        if (products.Count != productIds.Length || products.Values.Any(item => !item.IsActive))
        {
            throw SaleServiceException.Conflict("sale_product_unavailable", "Una o mas lineas corresponden a productos inexistentes o inactivos.");
        }

        foreach (var line in request.Items)
        {
            if (products[line.ProductId].CurrentStock < line.Quantity)
            {
                throw SaleServiceException.Conflict(
                    "insufficient_stock",
                    $"Existencia insuficiente para {products[line.ProductId].Name}. Consulta el stock e intenta de nuevo.");
            }
        }

        Sale sale;
        try
        {
            var saleItems = request.Items.Select(line => new SaleItem(products[line.ProductId], line.Quantity)).ToArray();
            sale = new Sale(CreateSaleNumber(), request.PaymentMethod, userId, saleItems);
            if (sale.Subtotal > MaximumMoneyValue || sale.Total > MaximumMoneyValue)
            {
                throw SaleServiceException.BadRequest("sale_total_too_large", "El total de la venta excede decimal(18,2).");
            }

            foreach (var line in request.Items)
            {
                try
                {
                    products[line.ProductId].ApplyStockAdjustment(-line.Quantity);
                }
                catch (InvalidOperationException)
                {
                    throw SaleServiceException.Conflict("insufficient_stock", "El stock cambio durante la venta. Consulta el carrito e intenta de nuevo.");
                }
            }
        }
        catch (OverflowException)
        {
            throw SaleServiceException.BadRequest("sale_total_too_large", "El total de la venta excede decimal(18,2).");
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw SaleServiceException.BadRequest("invalid_sale_line", exception.Message);
        }

        dbContext.Sales.Add(sale);
        try
        {
            // Save the generated SaleId while the transaction is open; it is used by stock movements and audit.
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var line in sale.Items.OrderBy(item => item.ProductId))
            {
                var product = products[line.ProductId];
                var after = product.CurrentStock;
                var before = checked(after + line.Quantity);
                dbContext.StockMovements.Add(new StockMovement(
                    line.ProductId,
                    MovementType.Sale,
                    -line.Quantity,
                    before,
                    after,
                    StockReferenceType.Sale,
                    sale.SaleId,
                    sale.SaleNumber,
                    userId));
            }

            dbContext.AuditLogs.Add(new AuditLog(
                userId,
                "SaleCreated",
                "Sale",
                sale.SaleId.ToString(),
                JsonSerializer.Serialize(new
                {
                    sale.SaleNumber,
                    sale.PaymentMethod,
                    sale.Subtotal,
                    sale.Discount,
                    sale.Total,
                    Items = sale.Items.Select(line => new
                    {
                        line.ProductId,
                        line.SKU,
                        line.ProductName,
                        line.Quantity,
                        line.UnitPrice,
                        line.LineTotal
                    })
                }, AuditJsonOptions),
                ipAddress));

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            throw SaleServiceException.Conflict(
                "sale_stock_changed",
                "Las existencias cambiaron durante la venta. Actualiza el carrito e intenta de nuevo.");
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw SaleServiceException.Conflict("sale_number_conflict", "No se pudo asignar un numero unico a la venta.");
        }
        catch (DbUpdateException exception) when (IsDeadlock(exception))
        {
            throw SaleServiceException.Conflict("sale_stock_changed", "La operacion coincidio con otra venta. Actualiza el carrito e intenta de nuevo.");
        }
        catch (SqlException exception) when (exception.Number == 1205)
        {
            throw SaleServiceException.Conflict("sale_stock_changed", "La operacion coincidio con otra venta. Actualiza el carrito e intenta de nuevo.");
        }

        return (await GetSaleAsync(sale.SaleId, userId, canViewAll: true, cancellationToken))!;
    }

    private static SaleDetailDto ToDetailDto(Sale sale) => new(
        sale.SaleId,
        sale.SaleNumber,
        sale.Status,
        sale.Subtotal,
        sale.Discount,
        sale.Total,
        sale.PaymentMethod,
        sale.CreatedByUserId,
        sale.CreatedAtUtc,
        sale.Items
            .OrderBy(item => item.SaleItemId)
            .Select(item => new SaleLineDto(
                item.SaleItemId,
                item.ProductId,
                item.SKU,
                item.ProductName,
                item.Quantity,
                item.UnitPrice,
                item.LineTotal))
            .ToArray());

    private static string CreateSaleNumber() =>
        $"SAL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..29].ToUpperInvariant();

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 2601 or 2627 };

    private static bool IsDeadlock(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 1205 };
}
