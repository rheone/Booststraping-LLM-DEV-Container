using System.Diagnostics;
using Depot.Domain;
using Depot.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Depot.Features.Shipments;

public static class DispatchShipment
{
    public abstract record Result
    {
        private Result()
        {
        }

        public sealed record Dispatched : Result;

        public sealed record NotFound : Result;

        public sealed record WrongState(ShipmentState State) : Result;
    }

    internal sealed class Handler(DepotDbContext db, TimeProvider clock)
    {
        public async Task<Result> HandleAsync(Guid id, CancellationToken ct)
        {
            Shipment? shipment = await db.Shipments.SingleOrDefaultAsync(s => s.Id == id, ct);
            if (shipment is null)
            {
                return new Result.NotFound();
            }

            if (shipment.State != ShipmentState.Registered)
            {
                return new Result.WrongState(shipment.State);
            }

            shipment.Dispatch(clock.GetUtcNow());
            await db.SaveChangesAsync(ct);
            return new Result.Dispatched();
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder shipments) =>
        shipments.MapPost("/{id:guid}/dispatch", HandleAsync).WithName(nameof(DispatchShipment));

    private static async Task<Results<NoContent, NotFound, Conflict<ProblemDetails>>> HandleAsync(
        Guid id, Handler handler, CancellationToken ct)
    {
        Result result = await handler.HandleAsync(id, ct);
        return result switch
        {
            Result.Dispatched => TypedResults.NoContent(),
            Result.NotFound => TypedResults.NotFound(),
            Result.WrongState w => TypedResults.Conflict(new ProblemDetails
            {
                Title = "Wrong state",
                Detail = $"A shipment in state {w.State} cannot be dispatched.",
                Status = StatusCodes.Status409Conflict,
            }),
            _ => throw new UnreachableException($"Unhandled result {result.GetType().Name}."),
        };
    }
}
