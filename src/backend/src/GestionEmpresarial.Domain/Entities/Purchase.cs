using GestionEmpresarial.Domain.Enums;

namespace GestionEmpresarial.Domain.Entities;

public sealed class Purchase
{
    public long PurchaseId { get; private set; }
    public string PurchaseNumber { get; private set; } = string.Empty;
    public int SupplierId { get; private set; }
    public Supplier Supplier { get; private set; } = null!;
    public PurchaseStatus Status { get; private set; } = PurchaseStatus.Draft;
    public decimal Subtotal { get; private set; }
    public decimal Total { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime? ReceivedAtUtc { get; private set; }
    public ICollection<PurchaseItem> Items { get; } = new List<PurchaseItem>();
}
