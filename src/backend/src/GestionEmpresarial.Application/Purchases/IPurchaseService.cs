namespace GestionEmpresarial.Application.Purchases;

public interface IPurchaseService
{
    Task<PurchasePageResult<PurchaseSummaryDto>> GetPurchasesAsync(
        PurchaseQuery query,
        CancellationToken cancellationToken);

    Task<PurchaseDetailDto?> GetPurchaseAsync(long purchaseId, CancellationToken cancellationToken);

    Task<PurchaseDetailDto> CreateDraftAsync(
        PurchaseWriteRequest request,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken);

    Task<PurchaseDetailDto?> ReceivePurchaseAsync(
        long purchaseId,
        string userId,
        string? ipAddress,
        CancellationToken cancellationToken);
}
