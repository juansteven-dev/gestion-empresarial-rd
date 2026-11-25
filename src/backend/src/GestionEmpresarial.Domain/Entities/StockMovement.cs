using GestionEmpresarial.Domain.Enums;

namespace GestionEmpresarial.Domain.Entities;

public sealed class StockMovement
{
    private StockMovement() { }

    public StockMovement(
        int productId,
        MovementType movementType,
        int quantityDelta,
        int stockBefore,
        int stockAfter,
        StockReferenceType referenceType,
        long? referenceId,
        string? notes,
        string createdByUserId)
    {
        if (productId <= 0 || quantityDelta == 0 || stockBefore < 0 || stockAfter < 0
            || (long)stockBefore + quantityDelta != stockAfter)
        {
            throw new ArgumentException("The stock movement must describe a nonzero change between valid balances.");
        }

        if (movementType == MovementType.Adjustment && string.IsNullOrWhiteSpace(notes))
        {
            throw new ArgumentException("An adjustment reason is required.", nameof(notes));
        }

        if (string.IsNullOrWhiteSpace(createdByUserId))
        {
            throw new ArgumentException("The actor is required.", nameof(createdByUserId));
        }

        ProductId = productId;
        MovementType = movementType;
        QuantityDelta = quantityDelta;
        StockBefore = stockBefore;
        StockAfter = stockAfter;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedByUserId = createdByUserId;
    }

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
