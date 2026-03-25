using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.API.Infrastructure.Persistence;
using Shared.Messaging.Events;

namespace Order.API.Features.Orders.Commands;

// ── Command ───────────────────────────────────────────────────────────────────
public record CancelOrderCommand(Guid OrderId, Guid UserId) : IRequest<bool>;

// ── Handler ───────────────────────────────────────────────────────────────────
public class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly OrderDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public CancelOrderCommandHandler(OrderDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken ct)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == request.UserId, ct);

        if (order is null) return false;

        order.Cancel();
        await _db.SaveChangesAsync(ct);

        await _publishEndpoint.Publish(
            new OrderCancelledEvent(order.Id, order.UserId, DateTime.UtcNow), ct);

        return true;
    }
}
