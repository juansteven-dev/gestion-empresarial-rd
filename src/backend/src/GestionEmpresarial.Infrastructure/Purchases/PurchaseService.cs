using System.Text.Json;
using GestionEmpresarial.Application.Purchases;
using GestionEmpresarial.Domain.Entities;
using GestionEmpresarial.Domain.Enums;
using GestionEmpresarial.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GestionEmpresarial.Infrastructure.Purchases;

public sealed class PurchaseService(ApplicationDbContext dbContext) : IPurchaseService
{
    private const decimal MaximumMoneyValue = 9999999999999999.99m;
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PurchasePageResult<PurchaseSummaryDto>> GetPurchasesAsync(
        PurchaseQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))
        {
            throw PurchaseServiceException.BadRequest("invalid_purchase_status", "El estado solicitado no es valido.");
        }

        var purchases = dbContext.Purchases.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            purchases = purchases.Where(purchase =>
                purchase.PurchaseNumber.Contains(search)
                || purchase.Supplier.Name.Contains(search));
        }

        if (query.SupplierId.HasValue)
        {
            purchases = purchases.Where(purchase => purchase.SupplierId == query.SupplierId.Value);
        }

        if (query.Status.HasValue)
        {
            purchases = purchases.Where(purchase => purchase.Status == query.Status.Value);
        }

        var totalCount = await purchases.CountAsync(cancellationToken);
        var skip = (int)Math.Min((long)(query.Page - 1) * query.PageSize, int.MaxValue);
        var items = await purchases
            .OrderByDescending(purchase => purchase.CreatedAtUtc)
            .ThenByDescending(purchase => purchase.PurchaseId)
            .Skip(skip)
            .Take(query.PageSize)
            .Select(purchase => new PurchaseSummaryDto(
                purchase.PurchaseId,
                purchase.PurchaseNumber,
                purchase.SupplierId,
                purchase.Supplier.Name,
                purchase.Status,
                purchase.Subtotal,
                purchase.Total,
                purchase.Items.Count,
                purchase.CreatedAtUtc,
                purchase.ReceivedAtUtc))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PurchasePageResult<PurchaseSummaryDto>(items, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<PurchaseDetailDto?> GetPurchaseAsync(long purchaseId, CancellationToken cancellationToken)
    {
        var purchase = await dbContext.Purchases
            .AsNoTracking()
            .Include(item => item.Supplier)
            .Include(item => item.Items)
            .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(item => item.PurchaseId == purchaseId, cancellationToken);

        return purchase is null ? null : ToDetailDto(purchase);
    }

    public async Task<PurchaseDetailDto> CreateDraftAsync(
        PurchaseWriteRequest request,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (request.Items is null || request.Items.Count is < 1 or > 100)
        {
            throw PurchaseServiceException.BadRequest(
                "purchase_items_required",
                "El borrador debe incluir entre 1 y 100 lineas.");
        }

        if (request.Items.Any(item => item is null))
        {
            throw PurchaseServiceException.BadRequest("invalid_purchase_line", "La compra no puede contener lineas vacias.");
        }

        var supplier = await dbContext.Suppliers.SingleOrDefaultAsync(
            item => item.SupplierId == request.SupplierId && item.IsActive,
            cancellationToken);
        if (supplier is null)
        {
            throw PurchaseServiceException.BadRequest("supplier_not_found", "Selecciona un suplidor activo que exista.");
        }

        var productIds = request.Items.Select(item => item.ProductId).Distinct().ToArray();
        var products = await dbContext.Products
            .Where(item => productIds.Contains(item.ProductId) && item.IsActive)
            .ToDictionaryAsync(item => item.ProductId, cancellationToken);
        if (products.Count != productIds.Length)
        {
            throw PurchaseServiceException.BadRequest("product_not_found", "Todas las lineas deben referirse a productos activos.");
        }

        List<PurchaseItem> items;
        try
        {
            items = request.Items.Select(item =>
            {
                if (item.ProductId <= 0 || item.Quantity <= 0 || item.UnitCost < 0
                    || item.UnitCost > MaximumMoneyValue
                    || decimal.Round(item.UnitCost, 2) != item.UnitCost)
                {
                    throw PurchaseServiceException.BadRequest(
                        "invalid_purchase_line",
                        "Cada linea requiere producto, cantidad positiva y costo no negativo con dos decimales como maximo.");
                }

                return new PurchaseItem(item.ProductId, item.Quantity, item.UnitCost);
            }).ToList();
        }
        catch (OverflowException)
        {
            throw PurchaseServiceException.BadRequest("purchase_total_too_large", "El total de la compra excede decimal(18,2).");
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw PurchaseServiceException.BadRequest("invalid_purchase_line", exception.Message);
        }

        var purchaseNumber = CreatePurchaseNumber();
        Purchase purchase;
        try
        {
            purchase = new Purchase(purchaseNumber, supplier.SupplierId, userId, items);
        }
        catch (OverflowException)
        {
            throw PurchaseServiceException.BadRequest("purchase_total_too_large", "El total de la compra excede decimal(18,2).");
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw PurchaseServiceException.BadRequest("purchase_total_too_large", exception.Message);
        }

        dbContext.Purchases.Add(purchase);
        AddAudit(userId, "PurchaseCreated", "Purchase", null, new
        {
            PurchaseNumber = purchase.PurchaseNumber,
            SupplierId = purchase.SupplierId,
            purchase.Subtotal,
            ItemCount = purchase.Items.Count
        }, ipAddress);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw PurchaseServiceException.Conflict("purchase_number_conflict", "No se pudo asignar un numero unico a la compra.");
        }

        return (await GetPurchaseAsync(purchase.PurchaseId, cancellationToken))!;
    }

    public async Task<PurchaseDetailDto?> ReceivePurchaseAsync(
        long purchaseId,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var purchase = await dbContext.Purchases
            .Include(item => item.Items)
            .ThenInclude(item => item.Product)
            .SingleOrDefaultAsync(item => item.PurchaseId == purchaseId, cancellationToken);
        if (purchase is null)
        {
            return null;
        }

        if (purchase.Status != PurchaseStatus.Draft)
        {
            throw PurchaseServiceException.Conflict("purchase_not_draft", "Solo se puede recibir una compra que siga en estado Draft.");
        }

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            if (transaction is not null)
            {
                var claimed = await dbContext.Purchases
                    .Where(item => item.PurchaseId == purchaseId && item.Status == PurchaseStatus.Draft)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(item => item.Status, PurchaseStatus.Received)
                            .SetProperty(item => item.ReceivedAtUtc, DateTime.UtcNow),
                        cancellationToken);
                if (claimed != 1)
                {
                    throw PurchaseServiceException.Conflict(
                        "purchase_not_draft",
                        "La compra ya fue recibida o cambio de estado.");
                }
            }

            var receivedAtUtc = DateTime.UtcNow;
            foreach (var item in purchase.Items.OrderBy(line => line.PurchaseItemId))
            {
                if (item.Product is null || !item.Product.IsActive)
                {
                    throw PurchaseServiceException.Conflict(
                        "purchase_product_inactive",
                        "No se puede recibir la compra porque una linea tiene un producto inactivo.");
                }

                (int Before, int After) balance;
                try
                {
                    balance = item.Product.ApplyStockAdjustment(item.Quantity);
                }
                catch (InvalidOperationException)
                {
                    throw PurchaseServiceException.Conflict(
                        "stock_limit_exceeded",
                        "La recepcion excede el maximo de existencias permitido.");
                }

                dbContext.StockMovements.Add(new StockMovement(
                    item.ProductId,
                    MovementType.Purchase,
                    item.Quantity,
                    balance.Before,
                    balance.After,
                    StockReferenceType.Purchase,
                    purchase.PurchaseId,
                    purchase.PurchaseNumber,
                    userId));
            }

            purchase.MarkReceived(receivedAtUtc);
            AddAudit(userId, "PurchaseReceived", "Purchase", purchase.PurchaseId.ToString(), new
            {
                purchase.PurchaseNumber,
                purchase.SupplierId,
                purchase.Total,
                Items = purchase.Items.Select(item => new
                {
                    item.ProductId,
                    item.Quantity,
                    item.UnitCost,
                    item.LineTotal
                })
            }, ipAddress);

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            throw PurchaseServiceException.Conflict(
                "purchase_or_stock_changed",
                "La compra o las existencias cambiaron durante la recepcion. Vuelve a consultar.");
        }

        return await GetPurchaseAsync(purchaseId, cancellationToken);
    }

    private static PurchaseDetailDto ToDetailDto(Purchase purchase) => new(
        purchase.PurchaseId,
        purchase.PurchaseNumber,
        purchase.SupplierId,
        purchase.Supplier.Name,
        purchase.Status,
        purchase.Subtotal,
        purchase.Total,
        purchase.CreatedByUserId,
        purchase.CreatedAtUtc,
        purchase.ReceivedAtUtc,
        purchase.Items
            .OrderBy(item => item.PurchaseItemId)
            .Select(item => new PurchaseLineDto(
                item.PurchaseItemId,
                item.ProductId,
                item.Product.SKU,
                item.Product.Name,
                item.Quantity,
                item.UnitCost,
                item.LineTotal))
            .ToArray());

    private static string CreatePurchaseNumber() =>
        $"PUR-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..25].ToUpperInvariant();

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 2601 or 2627 };

    private void AddAudit(string userId, string action, string entityName, string? entityId, object details, string? ipAddress) =>
        dbContext.AuditLogs.Add(new AuditLog(
            userId,
            action,
            entityName,
            entityId,
            JsonSerializer.Serialize(details, AuditJsonOptions),
            ipAddress));
}
