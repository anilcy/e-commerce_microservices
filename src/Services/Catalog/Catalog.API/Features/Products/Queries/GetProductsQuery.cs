using Catalog.API.Features.Products.Commands;
using Catalog.API.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Catalog.API.Features.Products.Queries;

// ── Get All (with optional category filter) ───────────────────────────────────
public record GetProductsQuery(Guid? CategoryId = null, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ProductDto>>;

public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);

public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    private readonly CatalogDbContext _db;

    public GetProductsQueryHandler(CatalogDbContext db) => _db = db;

    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        var query = _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive);

        if (request.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == request.CategoryId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductDto(p.Id, p.Name, p.Description,
                p.Price, p.Stock, p.Category.Name, p.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ProductDto>(items, total, request.Page, request.PageSize);
    }
}

// ── Get By Id ─────────────────────────────────────────────────────────────────
public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto?>;

public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly CatalogDbContext _db;

    public GetProductByIdQueryHandler(CatalogDbContext db) => _db = db;

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        return await _db.Products
            .Include(p => p.Category)
            .Where(p => p.Id == request.ProductId && p.IsActive)
            .Select(p => new ProductDto(p.Id, p.Name, p.Description,
                p.Price, p.Stock, p.Category.Name, p.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
