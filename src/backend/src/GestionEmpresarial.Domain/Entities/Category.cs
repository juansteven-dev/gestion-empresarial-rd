namespace GestionEmpresarial.Domain.Entities;

public sealed class Category
{
    public int CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public ICollection<Product> Products { get; } = new List<Product>();
}
