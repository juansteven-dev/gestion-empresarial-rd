namespace GestionEmpresarial.Domain.Entities;

public sealed class SaleItem
{
    private SaleItem() { }

    public SaleItem(Product product, int quantity)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (product.ProductId <= 0 || quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "A sale line needs a persisted product and a positive quantity.");
        }

        Product = product;
        ProductId = product.ProductId;
        ProductName = product.Name;
        SKU = product.SKU;
        Quantity = quantity;
        UnitPrice = product.SalePrice;
        LineTotal = checked(UnitPrice * quantity);
    }

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
