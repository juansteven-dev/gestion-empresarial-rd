using System.ComponentModel.DataAnnotations;
using GestionEmpresarial.Domain.Enums;

namespace GestionEmpresarial.Application.Catalogs;

public sealed record PageResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CategoryDto(int CategoryId, string Name, string? Description, bool IsActive);

public sealed class CategoryWriteRequest
{
    [Required]
    [StringLength(80, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class CategoryQuery
{
    [StringLength(120)]
    public string? Search { get; init; }

    public bool? IsActive { get; init; } = true;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record ProductDto(
    int ProductId,
    string SKU,
    string Name,
    string? Description,
    int CategoryId,
    string CategoryName,
    decimal CostPrice,
    decimal SalePrice,
    int CurrentStock,
    int MinimumStock,
    bool IsActive,
    DateTime UpdatedAtUtc);

public sealed class ProductWriteRequest
{
    [Required]
    [StringLength(40, MinimumLength = 1)]
    public string SKU { get; init; } = string.Empty;

    [Required]
    [StringLength(140, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; init; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal CostPrice { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal SalePrice { get; init; }

    [Range(0, int.MaxValue)]
    public int MinimumStock { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class ProductQuery
{
    [StringLength(160)]
    public string? Search { get; init; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }

    public bool? IsActive { get; init; } = true;
    public bool LowStockOnly { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record SupplierDto(
    int SupplierId,
    string Name,
    string? TaxId,
    string? Phone,
    string? Email,
    string? Address,
    bool IsActive);

public sealed class SupplierWriteRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(20)]
    public string? TaxId { get; init; }

    [StringLength(30)]
    public string? Phone { get; init; }

    [EmailAddress]
    [StringLength(254)]
    public string? Email { get; init; }

    [StringLength(250)]
    public string? Address { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class SupplierQuery
{
    [StringLength(160)]
    public string? Search { get; init; }

    public bool? IsActive { get; init; } = true;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed class StockMovementQuery
{
    [Range(1, int.MaxValue)]
    public int? ProductId { get; init; }

    public MovementType? Type { get; init; }
    public DateTime? FromUtc { get; init; }
    public DateTime? ToUtc { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed class StockAdjustmentRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; init; }

    [Range(-2147483647, 2147483647)]
    public int QuantityDelta { get; init; }

    [Required]
    [StringLength(250, MinimumLength = 3)]
    public string Reason { get; init; } = string.Empty;
}

public sealed record StockMovementDto(
    long StockMovementId,
    int ProductId,
    string SKU,
    string ProductName,
    MovementType Type,
    int QuantityDelta,
    int StockBefore,
    int StockAfter,
    string? Notes,
    string CreatedByUserId,
    DateTime CreatedAtUtc);

public sealed record StockAdjustmentResult(ProductDto Product, StockMovementDto Movement);
