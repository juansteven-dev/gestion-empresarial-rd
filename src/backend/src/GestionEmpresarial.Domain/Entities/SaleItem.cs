namespace GestionEmpresarial.Domain.Entities;

public sealed class SaleItem
{
    public long SaleItemId { get; private set; }
    public long SaleId { get; private set; }
    public Sale Sale { get; private set; } = null!;
    public int ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public string ProductName { get; private set; } = string.Empty;
    public string SKU { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal { get; private set; }
}
