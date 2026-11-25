using GestionEmpresarial.Domain.Enums;

namespace GestionEmpresarial.Domain.Entities;

public sealed class Sale
{
    private Sale() { }

    public Sale(string saleNumber, PaymentMethod paymentMethod, string createdByUserId, IReadOnlyCollection<SaleItem> items)
    {
        if (string.IsNullOrWhiteSpace(saleNumber) || saleNumber.Trim().Length > 30)
        {
            throw new ArgumentException("A sale number is required and must be at most 30 characters.", nameof(saleNumber));
        }

        if (!Enum.IsDefined(paymentMethod))
        {
            throw new ArgumentOutOfRangeException(nameof(paymentMethod));
        }

        if (string.IsNullOrWhiteSpace(createdByUserId))
        {
            throw new ArgumentException("A creator is required.", nameof(createdByUserId));
        }

        if (items is null || items.Count == 0)
        {
            throw new ArgumentException("A sale must contain at least one item.", nameof(items));
        }

        SaleNumber = saleNumber.Trim();
        PaymentMethod = paymentMethod;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = DateTime.UtcNow;
        foreach (var item in items)
        {
            Items.Add(item);
        }

        Subtotal = Items.Aggregate(0m, (total, item) => checked(total + item.LineTotal));
        Discount = 0m;
        Total = checked(Subtotal - Discount);
    }

    public long SaleId { get; private set; }
    public string SaleNumber { get; private set; } = string.Empty;
    public SaleStatus Status { get; private set; } = SaleStatus.Completed;
    public decimal Subtotal { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Total { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public ICollection<SaleItem> Items { get; } = new List<SaleItem>();
}
