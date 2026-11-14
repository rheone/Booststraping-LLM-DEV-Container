using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Store.Domain;
using Store.Infrastructure;

namespace Store.Features.Orders.CreateOrder;

public sealed record CreateOrderRequest(Guid CustomerId, decimal Total);

public sealed record OrderCreated(Guid OrderId);

public sealed record TotalNotPositive(decimal Total);

public union CreateOrderResult(OrderCreated, TotalNotPositive);

public sealed class CreateOrderHandler(StoreDbContext db, TimeProvider clock)
{
    public async Task<CreateOrderResult> HandleAsync(CreateOrderRequest request, CancellationToken ct)
    {
        if (request.Total <= 0)
        {
            return new TotalNotPositive(request.Total);
        }

        Order order = new() { Id = Guid.NewGuid(), CustomerId = request.CustomerId, Total = request.Total, PlacedAt = clock.GetUtcNow() };
        await order.SaveAsync(db, ct);
        return new OrderCreated(order.Id);
    }
}

public static class CreateOrderEndpoint
{
    public static void Map(RouteGroupBuilder orders) => orders.MapPost("/", HandleAsync);

    private static async Task<Results<Created<OrderCreated>, ValidationProblem>> HandleAsync(
        CreateOrderRequest request, CreateOrderHandler handler, CancellationToken ct)
    {
        CreateOrderResult result = await handler.HandleAsync(request, ct);
        return result switch
        {
            OrderCreated created => TypedResults.Created($"/orders/{created.OrderId}", created),
            TotalNotPositive => TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["Total"] = ["Must be positive."] }),
            null => throw new UnreachableException(),
        };
    }
}
