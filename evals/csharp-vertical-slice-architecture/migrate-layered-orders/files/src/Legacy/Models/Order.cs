namespace Legacy.Models;

public class Order
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = "";

    public string Status { get; set; } = "Open";

    public decimal Total { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ShippedAt { get; set; }
}
