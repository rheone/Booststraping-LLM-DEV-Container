using Depot.Domain;
using Microsoft.EntityFrameworkCore;

namespace Depot.Infrastructure;

public sealed class DepotDbContext(DbContextOptions<DepotDbContext> options) : DbContext(options)
{
    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shipment>(shipment =>
        {
            shipment.HasKey(s => s.Id);
            shipment.HasIndex(s => s.TrackingCode).IsUnique();
            shipment.Property(s => s.State).HasConversion<string>();
        });
    }
}
