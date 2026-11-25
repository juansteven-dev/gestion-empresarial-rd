namespace GestionEmpresarial.Domain.Entities;

public sealed class AuditLog
{
    public long AuditLogId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string? EntityId { get; private set; }
    public string? DetailsJson { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
}
