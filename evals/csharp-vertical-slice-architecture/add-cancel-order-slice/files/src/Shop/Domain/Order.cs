namespace Shop.Domain;

public enum OrderStatus
{
    Placed,
    Shipped,
}

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    private Order()
    {
    }

    public Guid Id { get; private set; }

    public string Number { get; private set; } = "";

    public Guid CustomerId { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTimeOffset PlacedAt { get; private set; }

    public DateTimeOffset? ShippedAt { get; private set; }

    public IReadOnlyList<OrderLine> Lines => _lines;

    public static Order Place(Guid customerId, IEnumerable<(string Sku, int Quantity, decimal UnitPrice)> lines, DateTimeOffset now)
    {
        Order order = new()
        {
            Id = Guid.CreateVersion7(now),
            Number = $"O-{now:yyyyMMddHHmmssfff}",
            CustomerId = customerId,
            Status = OrderStatus.Placed,
            PlacedAt = now,
        };

        foreach ((string sku, int quantity, decimal unitPrice) in lines)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
            order._lines.Add(new OrderLine(sku, quantity, unitPrice));
        }

        return order;
    }

    public void Ship(DateTimeOffset now)
    {
        Status = OrderStatus.Shipped;
        ShippedAt = now;
    }
}

public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice);
