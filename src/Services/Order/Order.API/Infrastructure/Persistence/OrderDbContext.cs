using Microsoft.EntityFrameworkCore;
using Order.API.Domain.Entities;

namespace Order.API.Infrastructure.Persistence;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    public DbSet<Order.API.Domain.Entities.Order> Orders => Set<Order.API.Domain.Entities.Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order.API.Domain.Entities.Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Status).HasConversion<string>();
            e.Property(o => o.TotalAmount).HasPrecision(18, 2);
            e.HasMany(o => o.Items)
             .WithOne()
             .HasForeignKey(i => i.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.UnitPrice).HasPrecision(18, 2);
            e.Property(i => i.ProductName).HasMaxLength(200);
        });
    }
}
