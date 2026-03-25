using FluentValidation;
using Identity.API.Domain.Entities;
using Identity.API.Infrastructure.Persistence;
using MassTransit;
using MediatR;
using Shared.Messaging.Events;

namespace Identity.API.Features.Auth.Commands;

// ── Command ──────────────────────────────────────────────────────────────────
public record RegisterCommand(
    string Email,
    string Username,
    string Password) : IRequest<RegisterResult>;

public record RegisterResult(Guid UserId, string Email, string Username);

// ── Validator ─────────────────────────────────────────────────────────────────

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Username).NotEmpty().MinimumLength(3).MaximumLength(50);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResult>
{
    private readonly IdentityDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public RegisterCommandHandler(IdentityDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken ct)
    {
        var emailExists = _db.Users.Any(u => u.Email == request.Email.ToLowerInvariant());
        if (emailExists)
            throw new InvalidOperationException("Email already registered.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = User.Create(request.Email, request.Username, passwordHash);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        // Publish integration event → other services can react
        await _publishEndpoint.Publish(new UserRegisteredEvent(
            user.Id, user.Email, user.Username, user.CreatedAt), ct);

        return new RegisterResult(user.Id, user.Email, user.Username);
    }
}
