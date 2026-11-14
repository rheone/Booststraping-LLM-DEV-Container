using Depot.Domain;
using Depot.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Depot.Tests;

public sealed class DepotFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<DepotDbContext>>();
            services.AddDbContext<DepotDbContext>(o => o.UseSqlite(_connection));
        });

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DepotDbContext>().Database.EnsureCreatedAsync();
    }

    public async Task<Guid> SeedAsync(Action<Shipment>? arrange = null)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        DepotDbContext db = scope.ServiceProvider.GetRequiredService<DepotDbContext>();
        Shipment shipment = Shipment.Register($"TRK{Guid.NewGuid():N}"[..12], "Oslo", DateTimeOffset.UtcNow);
        arrange?.Invoke(shipment);
        db.Shipments.Add(shipment);
        await db.SaveChangesAsync();
        return shipment.Id;
    }

    public override async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }
}
