using Catalog.API.Infrastructure.Persistence;
using MassTransit;
using MediatR;
using Shared.Messaging.Events;

namespace Catalog.API.Features.Products.Commands;

public record DeactivateProductCommand(Guid ProductId) : IRequest<bool>;

public class DeactivateProductCommandHandler : IRequestHandler<DeactivateProductCommand, bool>
{
    private readonly CatalogDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public DeactivateProductCommandHandler(CatalogDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<bool> Handle(DeactivateProductCommand request, CancellationToken ct)
    {
        var product = await _db.Products.FindAsync([request.ProductId], ct);
        if (product is null) return false;

        product.Deactivate();
        await _db.SaveChangesAsync(ct);

        // Order Service will mark this product as unavailable in its local copy
        await _publishEndpoint.Publish(
            new ProductDeactivatedEvent(product.Id, DateTime.UtcNow), ct);

        return true;
    }
}
