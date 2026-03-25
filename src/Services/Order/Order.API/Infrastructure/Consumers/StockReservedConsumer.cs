using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.API.Infrastructure.Persistence;
using Shared.Messaging.Events;

namespace Order.API.Infrastructure.Consumers;

/// <summary>
/// Listens for StockReservedEvent published by the Catalog Service.
/// Updates the order status based on whether stock reservation succeeded.
/// This closes the async saga loop: Order → Catalog → Order.
/// </summary>
public class StockReservedConsumer : IConsumer<StockReservedEvent>
{
    private readonly OrderDbContext _db;
    private readonly ILogger<StockReservedConsumer> _logger;

    public StockReservedConsumer(OrderDbContext db, ILogger<StockReservedConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        var @event = context.Message;

        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == @event.OrderId, context.CancellationToken);

        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found when processing StockReservedEvent", @event.OrderId);
            return;
        }

        if (@event.Success)
        {
            order.Confirm();
            _logger.LogInformation("Order {OrderId} confirmed after successful stock reservation", @event.OrderId);
        }
        else
        {
            order.MarkFailed();
            _logger.LogWarning("Order {OrderId} failed: {Reason}", @event.OrderId, @event.FailureReason);
        }

        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
