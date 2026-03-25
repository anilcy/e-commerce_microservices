using Catalog.API.Domain.Entities;
using Catalog.API.Infrastructure.Persistence;
using FluentValidation;
using MassTransit;
using MediatR;
using Shared.Messaging.Events;

namespace Catalog.API.Features.Products.Commands;

// ── Command ───────────────────────────────────────────────────────────────────
public record CreateProductCommand(
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    Guid CategoryId) : IRequest<ProductDto>;

public record ProductDto(
    Guid Id, string Name, string? Description,
    decimal Price, int Stock, string CategoryName, DateTime CreatedAt);

// ── Validator ─────────────────────────────────────────────────────────────────
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly CatalogDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public CreateProductCommandHandler(CatalogDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var category = await _db.Categories.FindAsync([request.CategoryId], ct)
            ?? throw new KeyNotFoundException($"Category {request.CategoryId} not found.");

        var product = Product.Create(
            request.Name, request.Description,
            request.Price, request.Stock, request.CategoryId);

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(new ProductCreatedEvent(
            product.Id, product.Name, product.Price, product.Stock, product.CreatedAt), ct);

        return new ProductDto(product.Id, product.Name, product.Description,
            product.Price, product.Stock, category.Name, product.CreatedAt);
    }
}
