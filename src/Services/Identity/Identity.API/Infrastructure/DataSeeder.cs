using Identity.API.Domain.Entities;
using Identity.API.Infrastructure.Persistence;

namespace Identity.API.Infrastructure;

/// <summary>
/// Runs once on startup. Creates a default Admin user if one doesn't exist.
/// This means you never need to manually touch the database to get an admin account.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(IdentityDbContext db)
    {
        // Only seed if no admin exists yet
        if (db.Users.Any(u => u.Role == "Admin"))
            return;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!");

        var admin = User.CreateAdmin(
            email: "admin@ecommerce.com",
            username: "admin",
            passwordHash: passwordHash);

        db.Users.Add(admin);
        await db.SaveChangesAsync();

        Console.WriteLine("✅ Admin user seeded: admin@ecommerce.com / Admin123!");
    }
}