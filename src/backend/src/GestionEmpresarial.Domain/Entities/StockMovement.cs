using GestionEmpresarial.Domain.Enums;

namespace GestionEmpresarial.Domain.Entities;

public sealed class StockMovement
{
    public long StockMovementId { get; private set; }
    public int ProductId { get; private set; }
    public Product Product { get; private set; } = null!;
    public MovementType MovementType { get; private set; }
    public int QuantityDelta { get; private set; }
    public int StockBefore { get; private set; }
    public int StockAfter { get; private set; }
    public StockReferenceType ReferenceType { get; private set; }
    public long? ReferenceId { get; private set; }
    public string? Notes { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
}
