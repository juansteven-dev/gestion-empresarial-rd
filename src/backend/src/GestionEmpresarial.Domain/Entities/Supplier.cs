namespace GestionEmpresarial.Domain.Entities;

public sealed class Supplier
{
    private Supplier() { }

    public Supplier(
        string name,
        string? taxId,
        string? phone,
        string? email,
        string? address,
        bool isActive = true)
    {
        Update(name, taxId, phone, email, address, isActive);
    }

    public int SupplierId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? TaxId { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public ICollection<Purchase> Purchases { get; } = new List<Purchase>();

    public void Update(string name, string? taxId, string? phone, string? email, string? address, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120)
        {
            throw new ArgumentException("A supplier name is required and must be at most 120 characters.", nameof(name));
        }

        Name = name.Trim();
        TaxId = Normalize(taxId);
        Phone = Normalize(phone);
        Email = Normalize(email);
        Address = Normalize(address);
        IsActive = isActive;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
