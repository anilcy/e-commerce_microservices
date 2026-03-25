namespace Identity.API.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Role { get; private set; } = "Customer";
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }

    private User() { } // EF Core

    public static User Create(string email, string username, string passwordHash)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            Username = username,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void Deactivate() => IsActive = false;
}
