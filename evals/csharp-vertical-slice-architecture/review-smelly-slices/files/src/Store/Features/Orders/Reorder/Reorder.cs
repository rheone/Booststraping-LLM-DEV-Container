using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Store.Features.Orders.CreateOrder;
using Store.Infrastructure;

namespace Store.Features.Orders.Reorder;

public sealed class ReorderHandler(StoreDbContext db, CreateOrderHandler createOrder)
{
    public async Task<CreateOrderResult?> HandleAsync(Guid previousOrderId, CancellationToken ct)
    {
        var previous = await db.Orders.AsNoTracking()
            .Where(o => o.Id == previousOrderId)
            .Select(o => new { o.CustomerId, o.Total })
            .SingleOrDefaultAsync(ct);

        if (previous is null)
        {
            return null;
        }

        return await createOrder.HandleAsync(new CreateOrderRequest(previous.CustomerId, previous.Total), ct);
    }
}

public static class ReorderEndpoint
{
    public static void Map(RouteGroupBuilder orders) => orders.MapPost("/{id:guid}/reorder", HandleAsync);

    private static async Task<Results<Created<OrderCreated>, NotFound, BadRequest>> HandleAsync(
        Guid id, ReorderHandler handler, CancellationToken ct)
    {
        CreateOrderResult? result = await handler.HandleAsync(id, ct);
        if (result is null)
        {
            return TypedResults.NotFound();
        }

        return result.Value.Value is OrderCreated created
            ? TypedResults.Created($"/orders/{created.OrderId}", created)
            : TypedResults.BadRequest();
    }
}
