using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Store.Common;
using Store.Infrastructure;

namespace Store.Features.Orders.ListOrders;

public sealed class ListOrdersHandler(StoreDbContext db)
{
    public async Task<IReadOnlyList<OrderDto>> HandleAsync(Guid customerId, CancellationToken ct)
    {
        var orders = await db.Orders.Where(o => o.CustomerId == customerId).ToListAsync(ct);
        return [.. orders.Select(o => new OrderDto(o.Id, o.CustomerId, o.Status, o.Total, o.PlacedAt))];
    }
}

public static class ListOrdersEndpoint
{
    public static void Map(RouteGroupBuilder orders) => orders.MapGet("/", HandleAsync);

    private static async Task<Ok<IReadOnlyList<OrderDto>>> HandleAsync(
        Guid customerId, ListOrdersHandler handler, CancellationToken ct) =>
        TypedResults.Ok(await handler.HandleAsync(customerId, ct));
}
