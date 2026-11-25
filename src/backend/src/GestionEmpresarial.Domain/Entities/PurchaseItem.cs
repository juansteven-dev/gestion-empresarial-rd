namespace GestionEmpresarial.Domain.Entities;

public sealed class PurchaseItem
{
    public long PurchaseItemId { get; private set; }
    public long PurchaseId { get; private set; }
    public Purchase Purchase { get; private set; } = null!;
    public int ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public int Quantity { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal LineTotal { get; private set; }
}
