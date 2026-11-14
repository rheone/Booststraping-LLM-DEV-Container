namespace Depot.Features.Shipments;

internal static class ShipmentsFeature
{
    public static IServiceCollection AddShipments(this IServiceCollection services) =>
        services
            .AddScoped<RegisterShipment.Handler>()
            .AddScoped<DispatchShipment.Handler>();

    public static IEndpointRouteBuilder MapShipments(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder shipments = app.MapGroup("/shipments").WithTags("Shipments");
        RegisterShipment.Map(shipments);
        DispatchShipment.Map(shipments);
        return app;
    }
}
