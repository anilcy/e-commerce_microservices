using Catalog.API.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Messaging.Events;

namespace Catalog.API.Infrastructure.Consumers;

/// <summary>
/// Listens for OrderPlacedEvent from the Order Service.
/// Attempts to reserve stock for each item, then publishes StockReservedEvent back.
/// This is async inter-service communication — no direct HTTP calls between services.
/// </summary>
public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly CatalogDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(CatalogDbContext db, IPublishEndpoint publishEndpoint,
        ILogger<OrderPlacedConsumer> logger)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var @event = context.Message;
        _logger.LogInformation("Processing stock reservation for Order {OrderId}", @event.OrderId);

        var productIds = @event.Items.Select(i => i.ProductId).ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(context.CancellationToken);

        foreach (var item in @event.Items)
        {
            var product = products.FirstOrDefault(p => p.Id == item.ProductId);
            if (product is null || !product.TryReserveStock(item.Quantity))
            {
                var reason = product is null ? "Product not found" : "Insufficient stock";
                _logger.LogWarning("Stock reservation failed for Order {OrderId}: {Reason}", @event.OrderId, reason);

                await _publishEndpoint.Publish(new StockReservedEvent(
                    @event.OrderId, Success: false, FailureReason: reason));
                return;
            }
        }

        await _db.SaveChangesAsync(context.CancellationToken);

        await _publishEndpoint.Publish(new StockReservedEvent(
            @event.OrderId, Success: true, FailureReason: null));

        _logger.LogInformation("Stock reserved successfully for Order {OrderId}", @event.OrderId);
    }
}
