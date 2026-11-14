using System.Net;

namespace Depot.Tests.Features.Shipments;

public sealed class DispatchShipmentTests(DepotFactory app) : IClassFixture<DepotFactory>
{
    [Fact]
    public async Task Dispatches_registered_shipment()
    {
        Guid id = await app.SeedAsync();

        HttpResponseMessage response = await app.CreateClient()
            .PostAsync($"/shipments/{id}/dispatch", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Returns_404_for_unknown_shipment()
    {
        HttpResponseMessage response = await app.CreateClient()
            .PostAsync($"/shipments/{Guid.NewGuid()}/dispatch", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Returns_409_when_already_dispatched()
    {
        Guid id = await app.SeedAsync(s => s.Dispatch(DateTimeOffset.UtcNow));

        HttpResponseMessage response = await app.CreateClient()
            .PostAsync($"/shipments/{id}/dispatch", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
