using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shop.Domain;
using Shop.Infrastructure.Persistence;

namespace Shop.IntegrationTests;

public sealed class ShopFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));
        });

    public async ValueTask InitializeAsync()
    {
        await _connection.OpenAsync();
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
    }

    public async Task<Guid> SeedOrderAsync(bool shipped = false)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Order order = Order.Place(Guid.NewGuid(), [("SKU-1", 1, 10m)], DateTimeOffset.UtcNow);
        if (shipped)
        {
            order.Ship(DateTimeOffset.UtcNow);
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order.Id;
    }

    public override async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
        await base.DisposeAsync();
    }
}
