namespace GestionEmpresarial.Domain.Entities;

public sealed class PurchaseItem
{
    private const decimal MaximumMoneyValue = 9999999999999999.99m;

    private PurchaseItem() { }

    public PurchaseItem(int productId, int quantity, decimal unitCost)
    {
        if (productId <= 0 || quantity <= 0 || unitCost < 0 || unitCost > MaximumMoneyValue)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Product, quantity and unit cost must be valid and nonnegative.");
        }

        if (decimal.Round(unitCost, 2) != unitCost)
        {
            throw new ArgumentOutOfRangeException(nameof(unitCost), "Unit cost supports at most two decimal places.");
        }

        var lineTotal = checked(quantity * unitCost);
        if (lineTotal > MaximumMoneyValue)
        {
            throw new ArgumentOutOfRangeException(nameof(unitCost), "The line total exceeds decimal(18,2).");
        }

        ProductId = productId;
        Quantity = quantity;
        UnitCost = unitCost;
        LineTotal = lineTotal;
    }

    public long PurchaseItemId { get; private set; }
    public long PurchaseId { get; private set; }
    public Purchase Purchase { get; private set; } = null!;
    public int ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal LineTotal { get; private set; }
}
