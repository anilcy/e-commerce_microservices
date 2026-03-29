using Catalog.API.Features.Categories.Commands;
using Catalog.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Features.Categories.Queries;

// ── Get All Categories ────────────────────────────────────────────────────────
public record GetCategoriesQuery : IRequest<List<CategoryDto>>;

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, List<CategoryDto>>
{
    private readonly CatalogDbContext _db;

    public GetCategoriesQueryHandler(CatalogDbContext db) => _db = db;

    public async Task<List<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        return await _db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description))
            .ToListAsync(ct);
    }
}

// ── Get Category By Id ────────────────────────────────────────────────────────
public record GetCategoryByIdQuery(Guid CategoryId) : IRequest<CategoryDto?>;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDto?>
{
    private readonly CatalogDbContext _db;

    public GetCategoryByIdQueryHandler(CatalogDbContext db) => _db = db;

    public async Task<CategoryDto?> Handle(GetCategoryByIdQuery request, CancellationToken ct)
    {
        return await _db.Categories
            .Where(c => c.Id == request.CategoryId)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Description))
            .FirstOrDefaultAsync(ct);
    }
}
