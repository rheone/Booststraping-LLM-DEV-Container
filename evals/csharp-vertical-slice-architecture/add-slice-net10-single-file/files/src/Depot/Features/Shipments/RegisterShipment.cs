using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Depot.Domain;
using Depot.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Depot.Features.Shipments;

public static class RegisterShipment
{
    public sealed record Request(
        [Required, StringLength(20, MinimumLength = 6)] string TrackingCode,
        [Required, StringLength(200)] string Destination);

    public sealed record Response(Guid Id, string TrackingCode);

    public abstract record Result
    {
        private Result()
        {
        }

        public sealed record Registered(Guid Id, string TrackingCode) : Result;

        public sealed record DuplicateTrackingCode(string TrackingCode) : Result;
    }

    internal sealed class Handler(DepotDbContext db, TimeProvider clock)
    {
        public async Task<Result> HandleAsync(Request request, CancellationToken ct)
        {
            if (await db.Shipments.AnyAsync(s => s.TrackingCode == request.TrackingCode, ct))
            {
                return new Result.DuplicateTrackingCode(request.TrackingCode);
            }

            Shipment shipment = Shipment.Register(request.TrackingCode, request.Destination, clock.GetUtcNow());
            db.Shipments.Add(shipment);
            await db.SaveChangesAsync(ct);
            return new Result.Registered(shipment.Id, shipment.TrackingCode);
        }
    }

    public static RouteHandlerBuilder Map(RouteGroupBuilder shipments) =>
        shipments.MapPost("/", HandleAsync).WithName(nameof(RegisterShipment));

    private static async Task<Results<Created<Response>, Conflict<ProblemDetails>>> HandleAsync(
        Request request, Handler handler, CancellationToken ct)
    {
        Result result = await handler.HandleAsync(request, ct);
        return result switch
        {
            Result.Registered r => TypedResults.Created($"/shipments/{r.Id}", new Response(r.Id, r.TrackingCode)),
            Result.DuplicateTrackingCode d => TypedResults.Conflict(new ProblemDetails
            {
                Title = "Duplicate tracking code",
                Detail = $"Tracking code '{d.TrackingCode}' is already registered.",
                Status = StatusCodes.Status409Conflict,
            }),
            _ => throw new UnreachableException($"Unhandled result {result.GetType().Name}."),
        };
    }
}
