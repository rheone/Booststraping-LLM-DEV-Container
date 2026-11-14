using Microsoft.EntityFrameworkCore;
using Store.Domain;

namespace Store.Infrastructure;

public sealed class StoreDbContext(DbContextOptions<StoreDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}

public sealed class NotFoundException(string message) : Exception(message);
