using System.ComponentModel.DataAnnotations;
using GestionEmpresarial.Domain.Enums;

namespace GestionEmpresarial.Application.Management;

public sealed class PeriodQuery
{
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
}

public sealed record DashboardSummaryDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal? SalesTotal,
    int? SaleCount,
    int? LowStockCount,
    IReadOnlyList<DashboardLowStockDto> LowStockProducts,
    IReadOnlyList<TopSellingProductDto> TopProducts);

public sealed record DashboardLowStockDto(int ProductId, string SKU, string Name, int CurrentStock, int MinimumStock);

public sealed record SalesByDayDto(DateTime DayUtc, int SaleCount, decimal Total);

public sealed record TopSellingProductDto(int ProductId, string SKU, string Name, int Quantity, decimal Total);

public sealed record MovementSummaryDto(MovementType Type, int MovementCount, int QuantityDelta);

public sealed record ReportsSummaryDto(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<SalesByDayDto> SalesByDay,
    IReadOnlyList<TopSellingProductDto> TopProducts,
    IReadOnlyList<MovementSummaryDto> Movements);

public sealed class AuditLogQuery
{
    [StringLength(80)]
    public string? Search { get; init; }

    [StringLength(450)]
    public string? UserId { get; init; }

    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record AuditLogDto(
    long AuditLogId,
    string UserId,
    string UserName,
    string Action,
    string EntityName,
    string? EntityId,
    string? DetailsJson,
    string? IpAddress,
    DateTime CreatedAtUtc);

public sealed class UserDirectoryQuery
{
    [StringLength(160)]
    public string? Search { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record UserDirectoryDto(
    string Id,
    string UserName,
    string? Email,
    IReadOnlyList<string> Roles,
    bool IsLockedOut);

public sealed record ManagementPageResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
