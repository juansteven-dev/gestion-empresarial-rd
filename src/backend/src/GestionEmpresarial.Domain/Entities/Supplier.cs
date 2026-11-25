namespace GestionEmpresarial.Domain.Entities;

public sealed class Supplier
{
    public int SupplierId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? TaxId { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public ICollection<Purchase> Purchases { get; } = new List<Purchase>();
}
