using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Store.Common;
using Store.Domain;
using Store.Infrastructure;

namespace Store.Features.Orders.CancelOrder;

public sealed record OrderCancelled(Guid OrderId, decimal Refund);

public sealed record CannotCancel(Guid OrderId, string Status);

public union CancelOrderResult(OrderCancelled, CannotCancel);

public sealed class CancelOrderHandler(StoreDbContext db, TimeProvider clock)
{
    public async Task<CancelOrderResult> HandleAsync(Guid orderId, CancellationToken ct)
    {
        Order order = await db.Orders.SingleOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException($"Order {orderId} not found");

        if (!OrderHelpers.CanCancel(order))
        {
            return new CannotCancel(orderId, order.Status);
        }

        decimal refund = OrderHelpers.CalculateRefund(order);
        order.Status = "Cancelled";
        order.CancelledAt = clock.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return new OrderCancelled(orderId, refund);
    }
}

public static class CancelOrderEndpoint
{
    public static void Map(RouteGroupBuilder orders) => orders.MapPost("/{id:guid}/cancel", HandleAsync);

    private static async Task<Results<Ok<OrderCancelled>, Conflict>> HandleAsync(
        Guid id, CancelOrderHandler handler, CancellationToken ct)
    {
        CancelOrderResult result = await handler.HandleAsync(id, ct);
        return result switch
        {
            OrderCancelled cancelled => TypedResults.Ok(cancelled),
            _ => TypedResults.Conflict(),
        };
    }
}
