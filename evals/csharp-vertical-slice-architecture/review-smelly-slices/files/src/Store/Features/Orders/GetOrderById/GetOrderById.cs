using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Store.Common;
using Store.Infrastructure;

namespace Store.Features.Orders.GetOrderById;

public static class GetOrderByIdEndpoint
{
    public static void Map(RouteGroupBuilder orders) => orders.MapGet("/{id:guid}", HandleAsync);

    private static async Task<Results<Ok<OrderDto>, NotFound>> HandleAsync(
        Guid id, StoreDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
        {
            return TypedResults.NotFound();
        }

        if (order.Status == "Placed" && order.PlacedAt < DateTimeOffset.UtcNow.AddDays(-30))
        {
            order.Status = "Expired";
            await db.SaveChangesAsync(ct);
        }

        return TypedResults.Ok(new OrderDto(order.Id, order.CustomerId, order.Status, order.Total, order.PlacedAt));
    }
}
