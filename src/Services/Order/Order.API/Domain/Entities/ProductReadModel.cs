namespace Order.API.Domain.Entities;

/// <summary>
/// A local read-only projection of products that Order Service maintains.
/// 
/// This is NOT the source of truth — Catalog Service owns that.
/// This exists purely so Order Service can look up product name and price
/// when placing an order, without making a synchronous HTTP call to Catalog.
/// 
/// It stays up to date by consuming ProductCreatedEvent, ProductUpdatedEvent,
/// and ProductDeactivatedEvent from RabbitMQ.
/// </summary>
public class ProductReadModel
{
    public Guid Id { get; private set; }          // same ID as in Catalog
    public string Name { get; private set; } = null!;
    public decimal Price { get; private set; }
    public bool IsAvailable { get; private set; }
    public DateTime LastSyncedAt { get; private set; }

    private ProductReadModel() { }

    public static ProductReadModel Create(Guid id, string name, decimal price) =>
        new()
        {
            Id = id,
            Name = name,
            Price = price,
            IsAvailable = true,
            LastSyncedAt = DateTime.UtcNow
        };

    public void UpdateDetails(string name, decimal price)
    {
        Name = name;
        Price = price;
        LastSyncedAt = DateTime.UtcNow;
    }

    public void MarkUnavailable()
    {
        IsAvailable = false;
        LastSyncedAt = DateTime.UtcNow;
    }
}
