using GestionEmpresarial.Domain.Enums;

namespace GestionEmpresarial.Domain.Entities;

public sealed class Sale
{
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
