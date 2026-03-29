using Catalog.API.Domain.Entities;
using Catalog.API.Infrastructure.Persistence;

namespace Catalog.API.Infrastructure;

/// <summary>
/// Seeds default categories on startup so the app is usable immediately
/// without manually inserting data.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(CatalogDbContext db)
    {
        if (db.Categories.Any())
            return;

        var categories = new[]
        {
            Category.Create("Electronics", "Phones, laptops, and gadgets"),
            Category.Create("Clothing", "Apparel and accessories"),
            Category.Create("Books", "Physical and digital books"),
            Category.Create("Home & Garden", "Furniture, tools, and decor"),
            Category.Create("Sports", "Equipment and sportswear"),
            Category.Create("General", "Miscellaneous items")
        };

        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        Console.WriteLine("✅ Default categories seeded.");
        foreach (var c in categories)
            Console.WriteLine($"   {c.Id}  →  {c.Name}");
    }
}