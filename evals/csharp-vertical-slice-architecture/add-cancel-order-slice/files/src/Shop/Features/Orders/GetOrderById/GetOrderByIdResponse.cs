namespace Shop.Features.Orders.GetOrderById;

public sealed record GetOrderByIdResponse(Guid Id, string Number, string Status, DateTimeOffset? ShippedAt, decimal Total);
