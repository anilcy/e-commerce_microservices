using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.API.Domain.Entities;
using Order.API.Infrastructure.Persistence;
using Shared.Messaging.Events;

namespace Order.API.Features.Orders.Commands;

// ── Command ───────────────────────────────────────────────────────────────────
// Notice: no ProductName or UnitPrice — Order Service resolves these itself
public record PlaceOrderCommand(
    Guid UserId,
    string? ShippingAddress,
    List<OrderItemRequest> Items) : IRequest<PlaceOrderResult>;

public record OrderItemRequest(
    Guid ProductId,
    int Quantity);  // ← only these two — no price, no name

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
        // ── Look up products from our local read model ─────────────────────
        var productIds = request.Items.Select(i => i.ProductId).ToList();

        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(ct);

        // Validate all requested products exist in our read model
        var missingIds = productIds.Except(products.Select(p => p.Id)).ToList();
        if (missingIds.Any())
            throw new KeyNotFoundException(
                $"Products not found: {string.Join(", ", missingIds)}. " +
                "Products must be created in Catalog before ordering.");

        // Validate none are deactivated
        var unavailable = products.Where(p => !p.IsAvailable).ToList();
        if (unavailable.Any())
            throw new InvalidOperationException(
                $"Products no longer available: {string.Join(", ", unavailable.Select(p => p.Name))}");

        // ── Build order items using our trusted local prices ───────────────
        var items = request.Items.Select(requested =>
        {
            var product = products.First(p => p.Id == requested.ProductId);
            return OrderItem.Create(
                productId: product.Id,
                productName: product.Name,   // from our read model
                quantity: requested.Quantity,
                unitPrice: product.Price);   // from our read model — client can't tamper
        }).ToList();

        var order = Order.API.Domain.Entities.Order.Create(
            request.UserId, request.ShippingAddress, items);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        // Publish event with the real prices we resolved
        await _publishEndpoint.Publish(new OrderPlacedEvent(
            order.Id,
            order.UserId,
            items.Select(i => new OrderPlacedItem(i.ProductId, i.Quantity, i.UnitPrice)).ToList(),
            order.TotalAmount,
            order.CreatedAt), ct);

        return new PlaceOrderResult(order.Id, order.Status.ToString(), order.TotalAmount);
    }
}
