using Shop.Features.Orders.CreateOrder;
using Shop.Features.Orders.GetOrderById;

namespace Shop.Features.Orders;

internal static class OrdersEndpoints
{
    public static IServiceCollection AddOrdersFeature(this IServiceCollection services) =>
        services
            .AddScoped<CreateOrderHandler>()
            .AddScoped<GetOrderByIdHandler>();

    public static IEndpointRouteBuilder MapOrders(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder orders = app.MapGroup("/orders").WithTags("Orders");
        CreateOrderEndpoint.Map(orders);
        GetOrderByIdEndpoint.Map(orders);
        return app;
    }
}
