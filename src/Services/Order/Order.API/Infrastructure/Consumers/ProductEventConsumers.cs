using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.API.Domain.Entities;
using Order.API.Infrastructure.Persistence;
using Shared.Messaging.Events;

namespace Order.API.Infrastructure.Consumers;

/// <summary>
/// Listens for ProductCreatedEvent from Catalog Service.
/// Creates a local copy of the product in Order's own database.
/// From this point on, Order Service knows this product exists and its price.
/// </summary>
public class ProductCreatedConsumer : IConsumer<ProductCreatedEvent>
{
    private readonly OrderDbContext _db;
    private readonly ILogger<ProductCreatedConsumer> _logger;

    public ProductCreatedConsumer(OrderDbContext db, ILogger<ProductCreatedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var @event = context.Message;

        // Idempotency check — don't create duplicates if event is delivered twice
        var exists = await _db.Products.AnyAsync(p => p.Id == @event.ProductId);
        if (exists)
        {
            _logger.LogWarning("Product {ProductId} already exists in read model, skipping", @event.ProductId);
            return;
        }

        var projection = ProductReadModel.Create(@event.ProductId, @event.Name, @event.Price);
        _db.Products.Add(projection);
        await _db.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Product projection created: {ProductId} - {Name} @ {Price}",
            @event.ProductId, @event.Name, @event.Price);
    }
}

/// <summary>
/// Listens for ProductUpdatedEvent from Catalog Service.
/// Updates the local copy when name or price changes.
/// Future orders will automatically use the new price.
/// </summary>
public class ProductUpdatedConsumer : IConsumer<ProductUpdatedEvent>
{
    private readonly OrderDbContext _db;
    private readonly ILogger<ProductUpdatedConsumer> _logger;

    public ProductUpdatedConsumer(OrderDbContext db, ILogger<ProductUpdatedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductUpdatedEvent> context)
    {
        var @event = context.Message;

        var projection = await _db.Products.FindAsync([@event.ProductId], context.CancellationToken);
        if (projection is null)
        {
            _logger.LogWarning("Product {ProductId} not found in read model during update", @event.ProductId);
            return;
        }

        projection.UpdateDetails(@event.Name, @event.Price);
        await _db.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Product projection updated: {ProductId} - {Name} @ {Price}",
            @event.ProductId, @event.Name, @event.Price);
    }
}

/// <summary>
/// Listens for ProductDeactivatedEvent from Catalog Service.
/// Marks product as unavailable — PlaceOrderCommand will reject orders
/// containing this product.
/// </summary>
public class ProductDeactivatedConsumer : IConsumer<ProductDeactivatedEvent>
{
    private readonly OrderDbContext _db;
    private readonly ILogger<ProductDeactivatedConsumer> _logger;

    public ProductDeactivatedConsumer(OrderDbContext db, ILogger<ProductDeactivatedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductDeactivatedEvent> context)
    {
        var @event = context.Message;

        var projection = await _db.Products.FindAsync([@event.ProductId], context.CancellationToken);
        if (projection is null) return;

        projection.MarkUnavailable();
        await _db.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Product {ProductId} marked unavailable in Order read model", @event.ProductId);
    }
}
