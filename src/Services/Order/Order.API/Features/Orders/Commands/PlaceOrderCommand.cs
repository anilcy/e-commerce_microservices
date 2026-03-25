using FluentValidation;
using MassTransit;
using MediatR;
using Order.API.Domain.Entities;
using Order.API.Infrastructure.Persistence;
using Shared.Messaging.Events;

namespace Order.API.Features.Orders.Commands;

// ── Command ───────────────────────────────────────────────────────────────────
public record PlaceOrderCommand(
    Guid UserId,
    string? ShippingAddress,
    List<OrderItemRequest> Items) : IRequest<PlaceOrderResult>;

public record OrderItemRequest(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice);

public record PlaceOrderResult(Guid OrderId, string Status, decimal TotalAmount);

// ── Validator ─────────────────────────────────────────────────────────────────
public class PlaceOrderCommandValidator : AbstractValidator<PlaceOrderCommand>
{
    public PlaceOrderCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Order must have at least one item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitPrice).GreaterThan(0);
        });
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, PlaceOrderResult>
{
    private readonly OrderDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public PlaceOrderCommandHandler(OrderDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<PlaceOrderResult> Handle(PlaceOrderCommand request, CancellationToken ct)
    {
        var items = request.Items
            .Select(i => OrderItem.Create(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
            .ToList();

        var order = Order.API.Domain.Entities.Order.Create(
            request.UserId, request.ShippingAddress, items);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        // Fire integration event — Catalog Service will listen and reserve stock
        await _publishEndpoint.Publish(new OrderPlacedEvent(
            order.Id,
            order.UserId,
            request.Items.Select(i => new OrderPlacedItem(i.ProductId, i.Quantity, i.UnitPrice)).ToList(),
            order.TotalAmount,
            order.CreatedAt), ct);

        return new PlaceOrderResult(order.Id, order.Status.ToString(), order.TotalAmount);
    }
}
