using Store.Infrastructure;

namespace Store.Domain;

public sealed class Order
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string Status { get; set; } = "Placed";

    public decimal Total { get; set; }

    public DateTimeOffset PlacedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public async Task SaveAsync(StoreDbContext db, CancellationToken ct)
    {
        if (db.Entry(this).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
        {
            db.Orders.Add(this);
        }

        await db.SaveChangesAsync(ct);
    }
}
