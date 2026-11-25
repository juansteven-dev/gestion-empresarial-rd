namespace GestionEmpresarial.Domain.Entities;

public sealed class Product
{
    private Product() { }

    public Product(
        string sku,
        string name,
        string? description,
        int categoryId,
        decimal costPrice,
        decimal salePrice,
        int minimumStock,
        bool isActive = true)
    {
        Update(sku, name, description, categoryId, costPrice, salePrice, minimumStock, isActive);
    }

    public int ProductId { get; private set; }
    public string SKU { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;
    public decimal CostPrice { get; private set; }
    public decimal SalePrice { get; private set; }
    public int CurrentStock { get; private set; }
    public int MinimumStock { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; private set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; private set; } = [];
    public ICollection<PurchaseItem> PurchaseItems { get; } = new List<PurchaseItem>();
    public ICollection<SaleItem> SaleItems { get; } = new List<SaleItem>();
    public ICollection<StockMovement> StockMovements { get; } = new List<StockMovement>();

    public void Update(
        string sku,
        string name,
        string? description,
        int categoryId,
        decimal costPrice,
        decimal salePrice,
        int minimumStock,
        bool isActive)
    {
        if (string.IsNullOrWhiteSpace(sku) || sku.Trim().Length > 40)
        {
            throw new ArgumentException("A SKU is required and must be at most 40 characters.", nameof(sku));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 140)
        {
            throw new ArgumentException("A product name is required and must be at most 140 characters.", nameof(name));
        }

        if (categoryId <= 0 || costPrice < 0 || salePrice < 0 || minimumStock < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(categoryId), "Category, prices and minimum stock must be valid and nonnegative.");
        }

        SKU = sku.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CategoryId = categoryId;
        CostPrice = costPrice;
        SalePrice = salePrice;
        MinimumStock = minimumStock;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public (int Before, int After) ApplyStockAdjustment(int quantityDelta)
    {
        if (quantityDelta == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityDelta), "A stock adjustment cannot be zero.");
        }

        var after = (long)CurrentStock + quantityDelta;
        if (after < 0 || after > int.MaxValue)
        {
            throw new InvalidOperationException("The adjustment would produce an invalid stock balance.");
        }

        var before = CurrentStock;
        CurrentStock = (int)after;
        UpdatedAtUtc = DateTime.UtcNow;
        return (before, CurrentStock);
    }
}
