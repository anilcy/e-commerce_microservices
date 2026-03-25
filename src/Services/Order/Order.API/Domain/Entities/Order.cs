namespace Order.API.Domain.Entities;

public enum OrderStatus
{
    Pending,        // Just placed, awaiting stock confirmation
    Confirmed,      // Stock reserved successfully
    Processing,     // Being fulfilled
    Shipped,
    Delivered,
    Cancelled,
    Failed          // Stock reservation failed
}

public class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string? ShippingAddress { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public ICollection<OrderItem> Items { get; private set; } = new List<OrderItem>();

    private Order() { }

    public static Order Create(Guid userId, string? shippingAddress, List<OrderItem> items)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = OrderStatus.Pending,
            ShippingAddress = shippingAddress,
            CreatedAt = DateTime.UtcNow,
            Items = items
        };
        order.TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity);
        return order;
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Only pending orders can be confirmed.");
        Status = OrderStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel a shipped or delivered order.");
        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = OrderStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = null!; // Snapshot at order time
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    private OrderItem() { }

    public static OrderItem Create(Guid productId, string productName, int quantity, decimal unitPrice) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
}
