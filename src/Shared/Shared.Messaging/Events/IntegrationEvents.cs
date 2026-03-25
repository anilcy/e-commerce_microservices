namespace Shared.Messaging.Events;

// Published by Identity Service
public record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string Username,
    DateTime RegisteredAt);

// Published by Order Service
public record OrderPlacedEvent(
    Guid OrderId,
    Guid UserId,
    List<OrderPlacedItem> Items,
    decimal TotalAmount,
    DateTime PlacedAt);

public record OrderPlacedItem(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice);

public record OrderCancelledEvent(
    Guid OrderId,
    Guid UserId,
    DateTime CancelledAt);

// Published by Catalog Service
public record StockReservedEvent(
    Guid OrderId,
    bool Success,
    string? FailureReason);

public record ProductCreatedEvent(
    Guid ProductId,
    string Name,
    decimal Price,
    int Stock,
    DateTime CreatedAt);
