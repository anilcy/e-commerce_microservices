using Identity.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Features.Users;

// ── Query ─────────────────────────────────────────────────────────────────────
public record GetUserByIdQuery(Guid UserId) : IRequest<UserDto?>;

public record UserDto(Guid Id, string Email, string Username, string Role, DateTime CreatedAt);

// ── Handler ───────────────────────────────────────────────────────────────────
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IdentityDbContext _db;

    public GetUserByIdQueryHandler(IdentityDbContext db) => _db = db;

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        return await _db.Users
            .Where(u => u.Id == request.UserId && u.IsActive)
            .Select(u => new UserDto(u.Id, u.Email, u.Username, u.Role, u.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
