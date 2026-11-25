namespace GestionEmpresarial.Application.Catalogs;

public interface ICatalogService
{
    Task<PageResult<CategoryDto>> GetCategoriesAsync(CategoryQuery query, CancellationToken cancellationToken);
    Task<CategoryDto?> GetCategoryAsync(int categoryId, CancellationToken cancellationToken);
    Task<CategoryDto> CreateCategoryAsync(CategoryWriteRequest request, string userId, string? ipAddress, CancellationToken cancellationToken);
    Task<CategoryDto?> UpdateCategoryAsync(int categoryId, CategoryWriteRequest request, string userId, string? ipAddress, CancellationToken cancellationToken);
    Task<bool> DeactivateCategoryAsync(int categoryId, string userId, string? ipAddress, CancellationToken cancellationToken);

    Task<PageResult<ProductDto>> GetProductsAsync(ProductQuery query, CancellationToken cancellationToken);
    Task<ProductDto?> GetProductAsync(int productId, CancellationToken cancellationToken);
    Task<ProductDto> CreateProductAsync(ProductWriteRequest request, string userId, string? ipAddress, CancellationToken cancellationToken);
    Task<ProductDto?> UpdateProductAsync(int productId, ProductWriteRequest request, string userId, string? ipAddress, CancellationToken cancellationToken);
    Task<bool> DeactivateProductAsync(int productId, string userId, string? ipAddress, CancellationToken cancellationToken);

    Task<PageResult<SupplierDto>> GetSuppliersAsync(SupplierQuery query, CancellationToken cancellationToken);
    Task<SupplierDto?> GetSupplierAsync(int supplierId, CancellationToken cancellationToken);
    Task<SupplierDto> CreateSupplierAsync(SupplierWriteRequest request, string userId, string? ipAddress, CancellationToken cancellationToken);
    Task<SupplierDto?> UpdateSupplierAsync(int supplierId, SupplierWriteRequest request, string userId, string? ipAddress, CancellationToken cancellationToken);
    Task<bool> DeactivateSupplierAsync(int supplierId, string userId, string? ipAddress, CancellationToken cancellationToken);

    Task<PageResult<ProductDto>> GetStockAsync(ProductQuery query, CancellationToken cancellationToken);
    Task<PageResult<ProductDto>> GetLowStockAsync(ProductQuery query, CancellationToken cancellationToken);
    Task<PageResult<StockMovementDto>> GetMovementsAsync(StockMovementQuery query, CancellationToken cancellationToken);
    Task<StockAdjustmentResult> AdjustStockAsync(StockAdjustmentRequest request, string userId, string? ipAddress, CancellationToken cancellationToken);
}
