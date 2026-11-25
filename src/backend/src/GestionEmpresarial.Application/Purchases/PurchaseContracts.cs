using System.ComponentModel.DataAnnotations;
using GestionEmpresarial.Domain.Enums;

namespace GestionEmpresarial.Application.Purchases;

public sealed record PurchasePageResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record PurchaseSummaryDto(
    long PurchaseId,
    string PurchaseNumber,
    int SupplierId,
    string SupplierName,
    PurchaseStatus Status,
    decimal Subtotal,
    decimal Total,
    int ItemCount,
    DateTime CreatedAtUtc,
    DateTime? ReceivedAtUtc);

public sealed record PurchaseLineDto(
    long PurchaseItemId,
    int ProductId,
    string SKU,
    string ProductName,
    int Quantity,
    decimal UnitCost,
    decimal LineTotal);

public sealed record PurchaseDetailDto(
    long PurchaseId,
    string PurchaseNumber,
    int SupplierId,
    string SupplierName,
    PurchaseStatus Status,
    decimal Subtotal,
    decimal Total,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    DateTime? ReceivedAtUtc,
    IReadOnlyList<PurchaseLineDto> Items);

public sealed class PurchaseLineWriteRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal UnitCost { get; init; }
}

public sealed class PurchaseWriteRequest
{
    [Range(1, int.MaxValue)]
    public int SupplierId { get; init; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<PurchaseLineWriteRequest> Items { get; init; } = [];
}

public sealed class PurchaseQuery
{
    [StringLength(80)]
    public string? Search { get; init; }

    [Range(1, int.MaxValue)]
    public int? SupplierId { get; init; }

    public PurchaseStatus? Status { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
