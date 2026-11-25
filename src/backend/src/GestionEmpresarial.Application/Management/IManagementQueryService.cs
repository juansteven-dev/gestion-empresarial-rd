namespace GestionEmpresarial.Application.Management;

public interface IManagementQueryService
{
    Task<DashboardSummaryDto> GetDashboardAsync(
        PeriodQuery query,
        string currentUserId,
        bool canViewSales,
        bool canViewAllSales,
        bool canViewInventory,
        CancellationToken cancellationToken);

    Task<ReportsSummaryDto> GetReportsAsync(PeriodQuery query, CancellationToken cancellationToken);

    Task<ManagementPageResult<AuditLogDto>> GetAuditLogsAsync(AuditLogQuery query, CancellationToken cancellationToken);

    Task<ManagementPageResult<UserDirectoryDto>> GetUsersAsync(UserDirectoryQuery query, CancellationToken cancellationToken);
}
