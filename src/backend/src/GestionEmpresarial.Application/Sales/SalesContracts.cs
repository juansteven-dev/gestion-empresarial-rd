using System.ComponentModel.DataAnnotations;
using GestionEmpresarial.Domain.Enums;

namespace GestionEmpresarial.Application.Sales;

public sealed record SalePageResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record SaleSummaryDto(
    long SaleId,
    string SaleNumber,
    SaleStatus Status,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    PaymentMethod PaymentMethod,
    int ItemCount,
    string CreatedByUserId,
    DateTime CreatedAtUtc);

public sealed record SaleLineDto(
    long SaleItemId,
    int ProductId,
    string SKU,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record SaleDetailDto(
    long SaleId,
    string SaleNumber,
    SaleStatus Status,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    PaymentMethod PaymentMethod,
    string CreatedByUserId,
    DateTime CreatedAtUtc,
    IReadOnlyList<SaleLineDto> Items);

public sealed class SaleLineWriteRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
}

public sealed class SaleWriteRequest
{
    [EnumDataType(typeof(PaymentMethod))]
    public PaymentMethod PaymentMethod { get; init; }

    [Required]
    [MinLength(1)]
    [MaxLength(100)]
    public List<SaleLineWriteRequest> Items { get; init; } = [];
}

public sealed class SaleQuery
{
    [StringLength(30)]
    public string? Search { get; init; }

    public DateOnly? FromDate { get; init; }

    public DateOnly? ToDate { get; init; }

    [StringLength(450)]
    public string? CreatedByUserId { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
