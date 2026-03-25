using Identity.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Identity.API.Features.Auth.Commands;

// ── Command ───────────────────────────────────────────────────────────────────
public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public record LoginResult(string AccessToken, string Email, string Username, string Role);

// ── Handler ───────────────────────────────────────────────────────────────────
public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IdentityDbContext _db;
    private readonly IConfiguration _config;

    public LoginCommandHandler(IdentityDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is deactivated.");

        var token = GenerateJwt(user.Id, user.Email, user.Username, user.Role);
        return new LoginResult(token, user.Email, user.Username, user.Role);
    }

    private string GenerateJwt(Guid userId, string email, string username, string role)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("username", username),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
