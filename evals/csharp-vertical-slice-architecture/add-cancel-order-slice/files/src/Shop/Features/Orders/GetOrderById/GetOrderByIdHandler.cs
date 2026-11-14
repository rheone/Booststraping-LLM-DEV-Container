using Microsoft.EntityFrameworkCore;
using Shop.Infrastructure.Persistence;

namespace Shop.Features.Orders.GetOrderById;

internal sealed class GetOrderByIdHandler(AppDbContext db)
{
    public async Task<GetOrderByIdResponse?> HandleAsync(Guid id, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new
            {
                o.Id,
                o.Number,
                o.Status,
                o.ShippedAt,
                Lines = o.Lines.Select(l => new { l.Quantity, l.UnitPrice }).ToList(),
            })
            .SingleOrDefaultAsync(ct);

        return order is null
            ? null
            : new GetOrderByIdResponse(
                order.Id,
                order.Number,
                order.Status.ToString(),
                order.ShippedAt,
                order.Lines.Sum(l => l.Quantity * l.UnitPrice));
    }
}
