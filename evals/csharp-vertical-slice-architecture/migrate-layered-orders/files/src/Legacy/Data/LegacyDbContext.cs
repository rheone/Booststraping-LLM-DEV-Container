using Legacy.Models;
using Microsoft.EntityFrameworkCore;

namespace Legacy.Data;

public class LegacyDbContext(DbContextOptions<LegacyDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
}
