using System.ComponentModel.DataAnnotations;

namespace Shop.Features.Orders.CreateOrder;

public sealed record CreateOrderRequest(
    [Required] Guid? CustomerId,
    [MinLength(1)] IReadOnlyList<CreateOrderLine> Lines);

public sealed record CreateOrderLine(
    [Required, StringLength(32)] string Sku,
    [Range(1, 1000)] int Quantity,
    [Range(0.01, 100000)] decimal UnitPrice);
