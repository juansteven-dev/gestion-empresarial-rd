using GestionEmpresarial.Domain.Enums;

namespace GestionEmpresarial.Domain.Entities;

public sealed class Purchase
{
    private const decimal MaximumMoneyValue = 9999999999999999.99m;

    private Purchase() { }

    public Purchase(
        string purchaseNumber,
        int supplierId,
        string createdByUserId,
        IReadOnlyCollection<PurchaseItem> items)
    {
        if (string.IsNullOrWhiteSpace(purchaseNumber) || purchaseNumber.Trim().Length > 30)
        {
            throw new ArgumentException("A purchase number is required and must be at most 30 characters.", nameof(purchaseNumber));
        }

        if (supplierId <= 0 || string.IsNullOrWhiteSpace(createdByUserId))
        {
            throw new ArgumentException("A valid supplier and creator are required.");
        }

        if (items.Count == 0)
        {
            throw new ArgumentException("A purchase must contain at least one item.", nameof(items));
        }

        var total = 0m;
        foreach (var item in items)
        {
            total = checked(total + item.LineTotal);
            if (total > MaximumMoneyValue)
            {
                throw new ArgumentOutOfRangeException(nameof(items), "The purchase total exceeds decimal(18,2).");
            }

            Items.Add(item);
        }

        PurchaseNumber = purchaseNumber.Trim();
        SupplierId = supplierId;
        CreatedByUserId = createdByUserId;
        Subtotal = total;
        Total = total;
    }

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

    public void MarkReceived(DateTime receivedAtUtc)
    {
        if (Status != PurchaseStatus.Draft || Items.Count == 0)
        {
            throw new InvalidOperationException("Only a non-empty draft purchase can be received.");
        }

        if (receivedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The receipt timestamp must be UTC.", nameof(receivedAtUtc));
        }

        Status = PurchaseStatus.Received;
        ReceivedAtUtc = receivedAtUtc;
    }
}
