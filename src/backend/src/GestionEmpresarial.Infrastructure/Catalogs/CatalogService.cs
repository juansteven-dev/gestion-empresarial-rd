using System.Text.Json;
using GestionEmpresarial.Application.Catalogs;
using GestionEmpresarial.Domain.Entities;
using GestionEmpresarial.Domain.Enums;
using GestionEmpresarial.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GestionEmpresarial.Infrastructure.Catalogs;

public sealed class CatalogService(ApplicationDbContext dbContext) : ICatalogService
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PageResult<CategoryDto>> GetCategoriesAsync(
        CategoryQuery query,
        CancellationToken cancellationToken)
    {
        var categories = dbContext.Categories.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            categories = categories.Where(category => category.Name.Contains(search));
        }

        if (query.IsActive.HasValue)
        {
            categories = categories.Where(category => category.IsActive == query.IsActive.Value);
        }

        return await PageAsync(
            categories.OrderBy(category => category.Name).ThenBy(category => category.CategoryId),
            query.Page,
            query.PageSize,
            category => new CategoryDto(category.CategoryId, category.Name, category.Description, category.IsActive),
            cancellationToken);
    }

    public Task<CategoryDto?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken) =>
        dbContext.Categories.AsNoTracking()
            .Where(category => category.CategoryId == categoryId)
            .Select(category => new CategoryDto(category.CategoryId, category.Name, category.Description, category.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<CategoryDto> CreateCategoryAsync(
        CategoryWriteRequest request,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        await EnsureCategoryNameAvailableAsync(name, null, cancellationToken);
        var category = new Category(name, request.Description, request.IsActive);
        dbContext.Categories.Add(category);
        AddAudit(userId, "CategoryCreated", "Category", null, new { category.Name }, ipAddress);
        await SaveChangesWithUniqueHandlingAsync("category_name_exists", "Ya existe una categoría con ese nombre.", cancellationToken);
        return new CategoryDto(category.CategoryId, category.Name, category.Description, category.IsActive);
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(
        int categoryId,
        CategoryWriteRequest request,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(
            item => item.CategoryId == categoryId,
            cancellationToken);
        if (category is null)
        {
            return null;
        }

        var name = request.Name.Trim();
        await EnsureCategoryNameAvailableAsync(name, categoryId, cancellationToken);
        if (!request.IsActive)
        {
            var hasActiveProducts = await dbContext.Products.AnyAsync(
                product => product.CategoryId == categoryId && product.IsActive,
                cancellationToken);
            if (hasActiveProducts)
            {
                throw CatalogServiceException.Conflict(
                    "category_has_active_products",
                    "Desactiva primero los productos activos de esta categoria.");
            }
        }

        category.Update(name, request.Description, request.IsActive);
        AddAudit(userId, "CategoryUpdated", "Category", categoryId.ToString(), new { category.Name, category.IsActive }, ipAddress);
        await SaveChangesWithUniqueHandlingAsync("category_name_exists", "Ya existe una categoría con ese nombre.", cancellationToken);
        return new CategoryDto(category.CategoryId, category.Name, category.Description, category.IsActive);
    }

    public async Task<bool> DeactivateCategoryAsync(
        int categoryId,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var category = await dbContext.Categories.SingleOrDefaultAsync(
            item => item.CategoryId == categoryId,
            cancellationToken);
        if (category is null)
        {
            return false;
        }

        var hasActiveProducts = await dbContext.Products.AnyAsync(
            product => product.CategoryId == categoryId && product.IsActive,
            cancellationToken);
        if (hasActiveProducts)
        {
            throw CatalogServiceException.Conflict(
                "category_has_active_products",
                "Desactiva primero los productos activos de esta categoría.");
        }

        category.Update(category.Name, category.Description, isActive: false);
        AddAudit(userId, "CategoryDeactivated", "Category", categoryId.ToString(), new { category.Name }, ipAddress);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<PageResult<ProductDto>> GetProductsAsync(ProductQuery query, CancellationToken cancellationToken) =>
        GetProductPageAsync(query, forceLowStockOnly: false, cancellationToken);

    public Task<PageResult<ProductDto>> GetStockAsync(ProductQuery query, CancellationToken cancellationToken) =>
        GetProductPageAsync(query, forceLowStockOnly: false, cancellationToken);

    public Task<PageResult<ProductDto>> GetLowStockAsync(ProductQuery query, CancellationToken cancellationToken) =>
        GetProductPageAsync(query, forceLowStockOnly: true, cancellationToken);

    public Task<ProductDto?> GetProductAsync(int productId, CancellationToken cancellationToken) =>
        ProductPageQuery()
            .Where(product => product.ProductId == productId)
            .Select(ToProductDto())
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ProductDto> CreateProductAsync(
        ProductWriteRequest request,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var sku = NormalizeSku(request.SKU);
        EnsureMoneyPrecision(request.CostPrice, request.SalePrice);
        await EnsureSkuAvailableAsync(sku, null, cancellationToken);
        await EnsureActiveCategoryExistsAsync(request.CategoryId, cancellationToken);

        var product = new Product(
            sku,
            request.Name,
            request.Description,
            request.CategoryId,
            request.CostPrice,
            request.SalePrice,
            request.MinimumStock,
            request.IsActive);
        dbContext.Products.Add(product);
        AddAudit(userId, "ProductCreated", "Product", null,
            new { product.SKU, product.Name, product.CategoryId, product.CostPrice, product.SalePrice }, ipAddress);
        await SaveChangesWithUniqueHandlingAsync("product_sku_exists", "Ya existe un producto con ese SKU.", cancellationToken);
        return (await GetProductAsync(product.ProductId, cancellationToken))!;
    }

    public async Task<ProductDto?> UpdateProductAsync(
        int productId,
        ProductWriteRequest request,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(
            item => item.ProductId == productId,
            cancellationToken);
        if (product is null)
        {
            return null;
        }

        var sku = NormalizeSku(request.SKU);
        EnsureMoneyPrecision(request.CostPrice, request.SalePrice);
        await EnsureSkuAvailableAsync(sku, productId, cancellationToken);
        await EnsureActiveCategoryExistsAsync(request.CategoryId, cancellationToken);

        product.Update(
            sku,
            request.Name,
            request.Description,
            request.CategoryId,
            request.CostPrice,
            request.SalePrice,
            request.MinimumStock,
            request.IsActive);
        AddAudit(userId, "ProductUpdated", "Product", productId.ToString(),
            new { product.SKU, product.Name, product.CategoryId, product.CostPrice, product.SalePrice, product.IsActive }, ipAddress);
        await SaveChangesWithUniqueHandlingAsync("product_sku_exists", "Ya existe un producto con ese SKU.", cancellationToken);
        return await GetProductAsync(productId, cancellationToken);
    }

    public async Task<bool> DeactivateProductAsync(
        int productId,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.SingleOrDefaultAsync(
            item => item.ProductId == productId,
            cancellationToken);
        if (product is null)
        {
            return false;
        }

        product.Update(
            product.SKU,
            product.Name,
            product.Description,
            product.CategoryId,
            product.CostPrice,
            product.SalePrice,
            product.MinimumStock,
            isActive: false);
        AddAudit(userId, "ProductDeactivated", "Product", productId.ToString(), new { product.SKU, product.Name }, ipAddress);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PageResult<SupplierDto>> GetSuppliersAsync(
        SupplierQuery query,
        CancellationToken cancellationToken)
    {
        var suppliers = dbContext.Suppliers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            suppliers = suppliers.Where(supplier =>
                supplier.Name.Contains(search)
                || (supplier.TaxId != null && supplier.TaxId.Contains(search))
                || (supplier.Email != null && supplier.Email.Contains(search)));
        }

        if (query.IsActive.HasValue)
        {
            suppliers = suppliers.Where(supplier => supplier.IsActive == query.IsActive.Value);
        }

        return await PageAsync(
            suppliers.OrderBy(supplier => supplier.Name).ThenBy(supplier => supplier.SupplierId),
            query.Page,
            query.PageSize,
            supplier => new SupplierDto(
                supplier.SupplierId,
                supplier.Name,
                supplier.TaxId,
                supplier.Phone,
                supplier.Email,
                supplier.Address,
                supplier.IsActive),
            cancellationToken);
    }

    public Task<SupplierDto?> GetSupplierAsync(int supplierId, CancellationToken cancellationToken) =>
        dbContext.Suppliers.AsNoTracking()
            .Where(supplier => supplier.SupplierId == supplierId)
            .Select(supplier => new SupplierDto(
                supplier.SupplierId,
                supplier.Name,
                supplier.TaxId,
                supplier.Phone,
                supplier.Email,
                supplier.Address,
                supplier.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<SupplierDto> CreateSupplierAsync(
        SupplierWriteRequest request,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var supplier = new Supplier(
            request.Name,
            request.TaxId,
            request.Phone,
            request.Email,
            request.Address,
            request.IsActive);
        dbContext.Suppliers.Add(supplier);
        AddAudit(userId, "SupplierCreated", "Supplier", null,
            new { supplier.Name, supplier.TaxId, supplier.Email }, ipAddress);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSupplierDto(supplier);
    }

    public async Task<SupplierDto?> UpdateSupplierAsync(
        int supplierId,
        SupplierWriteRequest request,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers.SingleOrDefaultAsync(
            item => item.SupplierId == supplierId,
            cancellationToken);
        if (supplier is null)
        {
            return null;
        }

        supplier.Update(request.Name, request.TaxId, request.Phone, request.Email, request.Address, request.IsActive);
        AddAudit(userId, "SupplierUpdated", "Supplier", supplierId.ToString(),
            new { supplier.Name, supplier.TaxId, supplier.Email, supplier.IsActive }, ipAddress);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSupplierDto(supplier);
    }

    public async Task<bool> DeactivateSupplierAsync(
        int supplierId,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var supplier = await dbContext.Suppliers.SingleOrDefaultAsync(
            item => item.SupplierId == supplierId,
            cancellationToken);
        if (supplier is null)
        {
            return false;
        }

        supplier.Update(supplier.Name, supplier.TaxId, supplier.Phone, supplier.Email, supplier.Address, isActive: false);
        AddAudit(userId, "SupplierDeactivated", "Supplier", supplierId.ToString(), new { supplier.Name }, ipAddress);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<PageResult<StockMovementDto>> GetMovementsAsync(
        StockMovementQuery query,
        CancellationToken cancellationToken)
    {
        if (query.FromUtc.HasValue && query.ToUtc.HasValue && query.FromUtc.Value > query.ToUtc.Value)
        {
            throw CatalogServiceException.BadRequest("invalid_date_range", "La fecha inicial debe ser anterior a la fecha final.");
        }

        var movements = dbContext.StockMovements.AsNoTracking().Include(movement => movement.Product).AsQueryable();
        if (query.ProductId.HasValue)
        {
            movements = movements.Where(movement => movement.ProductId == query.ProductId.Value);
        }

        if (query.Type.HasValue)
        {
            movements = movements.Where(movement => movement.MovementType == query.Type.Value);
        }

        if (query.FromUtc.HasValue)
        {
            movements = movements.Where(movement => movement.CreatedAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            movements = movements.Where(movement => movement.CreatedAtUtc <= query.ToUtc.Value);
        }

        return await PageAsync(
            movements.OrderByDescending(movement => movement.CreatedAtUtc).ThenByDescending(movement => movement.StockMovementId),
            query.Page,
            query.PageSize,
            ToMovementDto(),
            cancellationToken);
    }

    public async Task<StockAdjustmentResult> AdjustStockAsync(
        StockAdjustmentRequest request,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        if (request.QuantityDelta == 0)
        {
            throw CatalogServiceException.BadRequest("zero_stock_adjustment", "El ajuste debe cambiar la existencia en una cantidad distinta de cero.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw CatalogServiceException.BadRequest("adjustment_reason_required", "Indica el motivo del ajuste de inventario.");
        }

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var product = await dbContext.Products
                .Include(item => item.Category)
                .SingleOrDefaultAsync(item => item.ProductId == request.ProductId, cancellationToken);
            if (product is null)
            {
                throw CatalogServiceException.BadRequest("product_not_found", "El producto indicado no existe.");
            }

            if (!product.IsActive)
            {
                throw CatalogServiceException.Conflict("product_inactive", "No se puede ajustar un producto inactivo.");
            }

            (int Before, int After) balance;
            try
            {
                balance = product.ApplyStockAdjustment(request.QuantityDelta);
            }
            catch (InvalidOperationException)
            {
                throw CatalogServiceException.Conflict("negative_stock", "El ajuste no puede dejar existencias negativas.");
            }

            var reason = request.Reason.Trim();
            var movement = new StockMovement(
                product.ProductId,
                MovementType.Adjustment,
                request.QuantityDelta,
                balance.Before,
                balance.After,
                StockReferenceType.Manual,
                referenceId: null,
                reason,
                userId);
            dbContext.StockMovements.Add(movement);
            AddAudit(userId, "StockAdjusted", "Product", product.ProductId.ToString(), new
            {
                product.SKU,
                QuantityDelta = request.QuantityDelta,
                balance.Before,
                balance.After,
                Reason = reason
            }, ipAddress);

            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            var productDto = ToProductDto(product);
            var movementDto = new StockMovementDto(
                movement.StockMovementId,
                product.ProductId,
                product.SKU,
                product.Name,
                movement.MovementType,
                movement.QuantityDelta,
                movement.StockBefore,
                movement.StockAfter,
                movement.Notes,
                movement.CreatedByUserId,
                movement.CreatedAtUtc);
            return new StockAdjustmentResult(productDto, movementDto);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw CatalogServiceException.Conflict("stock_changed", "Las existencias cambiaron mientras procesabas el ajuste. Vuelve a consultar e inténtalo otra vez.");
        }
    }

    private async Task<PageResult<ProductDto>> GetProductPageAsync(
        ProductQuery query,
        bool forceLowStockOnly,
        CancellationToken cancellationToken)
    {
        var products = ProductPageQuery();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            products = products.Where(product => product.Name.Contains(search) || product.SKU.Contains(search));
        }

        if (query.CategoryId.HasValue)
        {
            products = products.Where(product => product.CategoryId == query.CategoryId.Value);
        }

        if (query.IsActive.HasValue)
        {
            products = products.Where(product => product.IsActive == query.IsActive.Value);
        }

        if (forceLowStockOnly || query.LowStockOnly)
        {
            products = products.Where(product => product.IsActive && product.CurrentStock <= product.MinimumStock);
        }

        return await PageAsync(
            products.OrderBy(product => product.Name).ThenBy(product => product.ProductId),
            query.Page,
            query.PageSize,
            ToProductDto(),
            cancellationToken);
    }

    private IQueryable<Product> ProductPageQuery() =>
        dbContext.Products.AsNoTracking().Include(product => product.Category);

    private async Task EnsureCategoryNameAvailableAsync(
        string name,
        int? exceptCategoryId,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.ToUpperInvariant();
        var exists = await dbContext.Categories.AnyAsync(
            category => category.Name.ToUpper() == normalizedName
                && (!exceptCategoryId.HasValue || category.CategoryId != exceptCategoryId.Value),
            cancellationToken);
        if (exists)
        {
            throw CatalogServiceException.Conflict("category_name_exists", "Ya existe una categoría con ese nombre.");
        }
    }

    private async Task EnsureSkuAvailableAsync(string sku, int? exceptProductId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Products.AnyAsync(
            product => product.SKU == sku
                && (!exceptProductId.HasValue || product.ProductId != exceptProductId.Value),
            cancellationToken);
        if (exists)
        {
            throw CatalogServiceException.Conflict("product_sku_exists", "Ya existe un producto con ese SKU.");
        }
    }

    private async Task EnsureActiveCategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Categories.AnyAsync(
            category => category.CategoryId == categoryId && category.IsActive,
            cancellationToken);
        if (!exists)
        {
            throw CatalogServiceException.BadRequest("category_not_found", "Selecciona una categoría activa que exista.");
        }
    }

    private async Task SaveChangesWithUniqueHandlingAsync(
        string code,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw CatalogServiceException.Conflict(code, message);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.GetBaseException() is SqlException { Number: 2601 or 2627 };

    private void AddAudit(
        string userId,
        string action,
        string entityName,
        string? entityId,
        object details,
        string? ipAddress) =>
        dbContext.AuditLogs.Add(new AuditLog(
            userId,
            action,
            entityName,
            entityId,
            JsonSerializer.Serialize(details, AuditJsonOptions),
            ipAddress));

    private static string NormalizeSku(string sku) => sku.Trim().ToUpperInvariant();

    private static void EnsureMoneyPrecision(decimal costPrice, decimal salePrice)
    {
        if (decimal.Round(costPrice, 2) != costPrice || decimal.Round(salePrice, 2) != salePrice)
        {
            throw CatalogServiceException.BadRequest(
                "invalid_money_precision",
                "Los precios admiten como maximo dos decimales.");
        }
    }

    private static SupplierDto ToSupplierDto(Supplier supplier) => new(
        supplier.SupplierId,
        supplier.Name,
        supplier.TaxId,
        supplier.Phone,
        supplier.Email,
        supplier.Address,
        supplier.IsActive);

    private static ProductDto ToProductDto(Product product) => new(
        product.ProductId,
        product.SKU,
        product.Name,
        product.Description,
        product.CategoryId,
        product.Category.Name,
        product.CostPrice,
        product.SalePrice,
        product.CurrentStock,
        product.MinimumStock,
        product.IsActive,
        product.UpdatedAtUtc);

    private static System.Linq.Expressions.Expression<Func<Product, ProductDto>> ToProductDto() => product => new ProductDto(
        product.ProductId,
        product.SKU,
        product.Name,
        product.Description,
        product.CategoryId,
        product.Category.Name,
        product.CostPrice,
        product.SalePrice,
        product.CurrentStock,
        product.MinimumStock,
        product.IsActive,
        product.UpdatedAtUtc);

    private static System.Linq.Expressions.Expression<Func<StockMovement, StockMovementDto>> ToMovementDto() => movement => new StockMovementDto(
        movement.StockMovementId,
        movement.ProductId,
        movement.Product.SKU,
        movement.Product.Name,
        movement.MovementType,
        movement.QuantityDelta,
        movement.StockBefore,
        movement.StockAfter,
        movement.Notes,
        movement.CreatedByUserId,
        movement.CreatedAtUtc);

    private static async Task<PageResult<TDto>> PageAsync<TEntity, TDto>(
        IOrderedQueryable<TEntity> query,
        int page,
        int pageSize,
        System.Linq.Expressions.Expression<Func<TEntity, TDto>> projection,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);
        var items = await query.Skip(skip).Take(pageSize).Select(projection).ToListAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PageResult<TDto>(items, page, pageSize, totalCount, totalPages);
    }
}
