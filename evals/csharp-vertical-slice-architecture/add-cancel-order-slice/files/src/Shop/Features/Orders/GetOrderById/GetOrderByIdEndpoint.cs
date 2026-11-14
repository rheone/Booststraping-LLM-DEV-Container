using Microsoft.AspNetCore.Http.HttpResults;

namespace Shop.Features.Orders.GetOrderById;

internal static class GetOrderByIdEndpoint
{
    public static RouteHandlerBuilder Map(RouteGroupBuilder orders) =>
        orders.MapGet("/{id:guid}", HandleAsync)
            .WithName("GetOrderById")
            .WithSummary("Get one order.");

    private static async Task<Results<Ok<GetOrderByIdResponse>, NotFound>> HandleAsync(
        Guid id, GetOrderByIdHandler handler, CancellationToken ct) =>
        await handler.HandleAsync(id, ct) is { } order ? TypedResults.Ok(order) : TypedResults.NotFound();
}
