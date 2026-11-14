using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Shop.Features.Orders.CreateOrder;

internal static class CreateOrderEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder orders) =>
        orders.MapPost("/", HandleAsync)
            .WithName("CreateOrder")
            .WithSummary("Place a new order.");

    private static async Task<Results<Created<OrderCreated>, ValidationProblem>> HandleAsync(
        CreateOrderRequest request, CreateOrderHandler handler, CancellationToken ct)
    {
        CreateOrderResult result = await handler.HandleAsync(request, ct);
        return result switch
        {
            OrderCreated created => TypedResults.Created($"/orders/{created.OrderId}", created),
            DuplicateSkus duplicates => TypedResults.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["Lines"] = [.. duplicates.Skus.Select(s => $"SKU '{s}' appears more than once.")],
                }),
            null => throw new UnreachableException("CreateOrderHandler returned a default result."),
        };
    }
}
