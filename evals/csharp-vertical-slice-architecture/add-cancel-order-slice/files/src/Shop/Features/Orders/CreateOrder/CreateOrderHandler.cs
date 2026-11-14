using Shop.Domain;
using Shop.Infrastructure.Persistence;

namespace Shop.Features.Orders.CreateOrder;

internal sealed class CreateOrderHandler(AppDbContext db, TimeProvider clock)
{
    public async Task<CreateOrderResult> HandleAsync(CreateOrderRequest request, CancellationToken ct)
    {
        string[] duplicates = [.. request.Lines.GroupBy(l => l.Sku).Where(g => g.Count() > 1).Select(g => g.Key)];
        if (duplicates.Length > 0)
        {
            return new DuplicateSkus(duplicates);
        }

        Order order = Order.Place(
            request.CustomerId!.Value,
            request.Lines.Select(l => (l.Sku, l.Quantity, l.UnitPrice)),
            clock.GetUtcNow());

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        return new OrderCreated(order.Id, order.Number);
    }
}
