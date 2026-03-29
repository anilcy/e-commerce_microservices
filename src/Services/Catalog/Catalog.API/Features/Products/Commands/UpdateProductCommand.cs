using Catalog.API.Infrastructure.Persistence;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Messaging.Events;

namespace Catalog.API.Features.Products.Commands;

// ── Command ───────────────────────────────────────────────────────────────────
public record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string? Description,
    decimal Price) : IRequest<ProductDto?>;

// ── Validator ─────────────────────────────────────────────────────────────────
public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto?>
{
    private readonly CatalogDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public UpdateProductCommandHandler(CatalogDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<ProductDto?> Handle(UpdateProductCommand request, CancellationToken ct)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);

        if (product is null) return null;

        product.UpdateDetails(request.Name, request.Description, request.Price);
        await _db.SaveChangesAsync(ct);

        // Notify Order Service (and any other interested services) that
        // this product's name/price changed — they update their local copies
        await _publishEndpoint.Publish(new ProductUpdatedEvent(
            product.Id,
            product.Name,
            product.Price,
            DateTime.UtcNow), ct);

        return new ProductDto(product.Id, product.Name, product.Description,
            product.Price, product.Stock, product.Category.Name, product.CreatedAt);
    }
}
