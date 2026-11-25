namespace GestionEmpresarial.Domain.Entities;

public sealed class Product
{
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
}
