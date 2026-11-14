using System.Net;
using System.Net.Http.Json;
using Shop.Features.Orders.CreateOrder;

namespace Shop.IntegrationTests.Features.Orders.CreateOrder;

public sealed class CreateOrderTests(ShopFactory app) : IClassFixture<ShopFactory>
{
    [Fact]
    public async Task Creates_order()
    {
        HttpResponseMessage response = await app.CreateClient().PostAsJsonAsync(
            "/orders",
            new CreateOrderRequest(Guid.NewGuid(), [new CreateOrderLine("SKU-1", 2, 10m)]),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_duplicate_skus()
    {
        HttpResponseMessage response = await app.CreateClient().PostAsJsonAsync(
            "/orders",
            new CreateOrderRequest(Guid.NewGuid(), [new CreateOrderLine("SKU-1", 1, 10m), new CreateOrderLine("SKU-1", 1, 10m)]),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Rejects_empty_lines()
    {
        HttpResponseMessage response = await app.CreateClient().PostAsJsonAsync(
            "/orders",
            new CreateOrderRequest(Guid.NewGuid(), []),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
