using Catalog.API.Domain.Entities;
using Catalog.API.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Features.Categories.Commands;

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record CategoryDto(Guid Id, string Name, string? Description);

// ── Create Category ───────────────────────────────────────────────────────────
public record CreateCategoryCommand(string Name, string? Description)
    : IRequest<CategoryDto>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly CatalogDbContext _db;

    public CreateCategoryCommandHandler(CatalogDbContext db) => _db = db;

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        var category = Category.Create(request.Name, request.Description);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(ct);
        return new CategoryDto(category.Id, category.Name, category.Description);
    }
}
