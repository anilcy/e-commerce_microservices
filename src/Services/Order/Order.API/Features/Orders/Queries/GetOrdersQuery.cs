using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.API.Domain.Entities;
using Order.API.Infrastructure.Persistence;

namespace Order.API.Features.Orders.Queries;

// ── DTOs ──────────────────────────────────────────────────────────────────────
public record OrderDto(
    Guid Id, Guid UserId, string Status,
    decimal TotalAmount, string? ShippingAddress,
    DateTime CreatedAt, List<OrderItemDto> Items);

public record OrderItemDto(
    Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

// ── Get All Orders For User ───────────────────────────────────────────────────
public record GetOrdersByUserQuery(Guid UserId) : IRequest<List<OrderDto>>;

public class GetOrdersByUserQueryHandler : IRequestHandler<GetOrdersByUserQuery, List<OrderDto>>
{
    private readonly OrderDbContext _db;

    public GetOrdersByUserQueryHandler(OrderDbContext db) => _db = db;

    public async Task<List<OrderDto>> Handle(GetOrdersByUserQuery request, CancellationToken ct)
    {
        return await _db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == request.UserId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => MapToDto(o))
            .ToListAsync(ct);
    }

    private static OrderDto MapToDto(Order.API.Domain.Entities.Order o) =>
        new(o.Id, o.UserId, o.Status.ToString(), o.TotalAmount, o.ShippingAddress,
            o.CreatedAt, o.Items.Select(i =>
                new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList());
}

// ── Get Single Order ──────────────────────────────────────────────────────────
public record GetOrderByIdQuery(Guid OrderId, Guid UserId) : IRequest<OrderDto?>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly OrderDbContext _db;

    public GetOrderByIdQueryHandler(OrderDbContext db) => _db = db;

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.UserId == request.UserId, ct);

        if (order is null) return null;

        return new OrderDto(order.Id, order.UserId, order.Status.ToString(),
            order.TotalAmount, order.ShippingAddress, order.CreatedAt,
            order.Items.Select(i =>
                new OrderItemDto(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList());
    }
}
