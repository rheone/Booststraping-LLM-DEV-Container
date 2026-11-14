namespace Depot.Domain;

public enum ShipmentState
{
    Registered,
    Dispatched,
    Delivered,
}

public sealed class Shipment
{
    private Shipment()
    {
    }

    public Guid Id { get; private set; }

    public string TrackingCode { get; private set; } = "";

    public string Destination { get; private set; } = "";

    public ShipmentState State { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    public DateTimeOffset? DispatchedAt { get; private set; }

    public DateTimeOffset? DeliveredAt { get; private set; }

    public static Shipment Register(string trackingCode, string destination, DateTimeOffset now) => new()
    {
        Id = Guid.CreateVersion7(now),
        TrackingCode = trackingCode,
        Destination = destination,
        State = ShipmentState.Registered,
        RegisteredAt = now,
    };

    public void Dispatch(DateTimeOffset now)
    {
        if (State != ShipmentState.Registered)
        {
            throw new InvalidOperationException($"Cannot dispatch a shipment in state {State}.");
        }

        State = ShipmentState.Dispatched;
        DispatchedAt = now;
    }
}
