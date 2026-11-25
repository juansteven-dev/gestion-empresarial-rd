namespace GestionEmpresarial.Application.Sales;

public interface ISaleService
{
    Task<SalePageResult<SaleSummaryDto>> GetSalesAsync(
        SaleQuery query,
        string currentUserId,
        bool canViewAll,
        CancellationToken cancellationToken);

    Task<SaleDetailDto?> GetSaleAsync(
        long saleId,
        string currentUserId,
        bool canViewAll,
        CancellationToken cancellationToken);

    Task<SaleDetailDto> CreateSaleAsync(
        SaleWriteRequest request,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken);
}
