using Microsoft.AspNetCore.Http.HttpResults;
using Store.Features.Orders.ListOrders;

namespace Store.Features.Billing.CreateInvoice;

public sealed record InvoiceCreated(Guid CustomerId, decimal Amount, int OrderCount);

public sealed class CreateInvoiceHandler(ListOrdersHandler listOrders)
{
    public async Task<InvoiceCreated> HandleAsync(Guid customerId, CancellationToken ct)
    {
        var orders = await listOrders.HandleAsync(customerId, ct);
        var billable = orders.Where(o => o.Status == "Placed").ToList();
        return new InvoiceCreated(customerId, billable.Sum(o => o.Total), billable.Count);
    }
}

public static class CreateInvoiceEndpoint
{
    public static void Map(RouteGroupBuilder invoices) => invoices.MapPost("/{customerId:guid}", HandleAsync);

    private static async Task<Ok<InvoiceCreated>> HandleAsync(
        Guid customerId, CreateInvoiceHandler handler, CancellationToken ct) =>
        TypedResults.Ok(await handler.HandleAsync(customerId, ct));
}
