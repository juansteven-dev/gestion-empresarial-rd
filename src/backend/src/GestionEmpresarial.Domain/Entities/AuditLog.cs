namespace GestionEmpresarial.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog() { }

    public AuditLog(
        string userId,
        string action,
        string entityName,
        string? entityId,
        string? detailsJson,
        string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(userId)
            || string.IsNullOrWhiteSpace(action)
            || action.Trim().Length > 80
            || string.IsNullOrWhiteSpace(entityName)
            || entityName.Trim().Length > 80
            || entityId?.Length > 80
            || ipAddress?.Length > 45)
        {
            throw new ArgumentException("Audit actor and entity details are required and must fit the database limits.");
        }

        UserId = userId;
        Action = action.Trim();
        EntityName = entityName.Trim();
        EntityId = entityId;
        DetailsJson = AuditDetailsSanitizer.Sanitize(detailsJson);
        IpAddress = ipAddress;
    }

    public long AuditLogId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string? DetailsJson { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
}
