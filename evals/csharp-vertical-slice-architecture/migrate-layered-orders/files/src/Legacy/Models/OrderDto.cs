namespace Legacy.Models;

public class OrderDto
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = "";

    public string Status { get; set; } = "";

    public decimal Total { get; set; }

    public DateTime? ShippedAt { get; set; }
}

public class CreateOrderDto
{
    public string CustomerName { get; set; } = "";

    public decimal Total { get; set; }
}
