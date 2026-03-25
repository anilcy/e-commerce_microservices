namespace Catalog.API.Domain.Entities;

public class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public ICollection<Product> Products { get; private set; } = new List<Product>();

    private Category() { }

    public static Category Create(string name, string? description = null) =>
        new() { Id = Guid.NewGuid(), Name = name, Description = description };
}

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;

    private Product() { }

    public static Product Create(string name, string? description, decimal price, int stock, Guid categoryId) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Price = price,
            Stock = stock,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CategoryId = categoryId
        };

    public void UpdateDetails(string name, string? description, decimal price)
    {
        Name = name;
        Description = description;
        Price = price;
    }

    public bool TryReserveStock(int quantity)
    {
        if (Stock < quantity) return false;
        Stock -= quantity;
        return true;
    }

    public void RestoreStock(int quantity) => Stock += quantity;

    public void Deactivate() => IsActive = false;
}
