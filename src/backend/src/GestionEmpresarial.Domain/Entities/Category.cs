namespace GestionEmpresarial.Domain.Entities;

public sealed class Category
{
    private Category() { }

    public Category(string name, string? description, bool isActive = true)
    {
        Update(name, description, isActive);
    }

    public int CategoryId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public ICollection<Product> Products { get; } = new List<Product>();

    public void Update(string name, string? description, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80)
        {
            throw new ArgumentException("A category name is required and must be at most 80 characters.", nameof(name));
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = isActive;
    }
}
