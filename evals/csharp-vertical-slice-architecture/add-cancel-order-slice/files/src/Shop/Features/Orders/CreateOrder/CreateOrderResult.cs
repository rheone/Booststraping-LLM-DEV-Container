namespace Shop.Features.Orders.CreateOrder;

public sealed record OrderCreated(Guid OrderId, string OrderNumber);

public sealed record DuplicateSkus(IReadOnlyList<string> Skus);

public union CreateOrderResult(OrderCreated, DuplicateSkus);
