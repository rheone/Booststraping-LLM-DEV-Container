# Core Concepts

## DbContext and DbSet\<T>

A `DbContext` represents a session with the database — a unit of work that tracks the entities you
load and computes the changes `SaveChanges` needs to persist. You declare one `DbSet<T>` property
per entity type you query or persist directly:

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().HasKey(o => o.Id);
        modelBuilder.Entity<Customer>().HasKey(c => c.Id);
    }
}
```

`Set<T>()` and an auto-property (`DbSet<Order> Orders => Set<Order>()`) behave identically to a
settable auto-property (`public DbSet<Order> Orders { get; set; }`); the expression-bodied form
avoids leaving a public setter that nothing needs to call.

A `DbContext` instance is not thread-safe and is not meant to be long-lived — you create one per
unit of work (per web request, per background job iteration) and dispose it when the work
completes. Do not cache a `DbContext` instance across requests or share one instance across
concurrent operations.

## OnConfiguring vs. dependency-injection registration

`OnConfiguring` configures the context from inside itself, hard-coding (or reading from
`Environment` variables) the provider and connection string:

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseSqlServer("Server=.;Database=AppDb;Trusted_Connection=True;");
}
```

This works for a console app, a design-time factory, or a quick prototype, but it couples the
context to one specific configuration source and makes substituting a different connection string
or provider per environment (test vs. staging vs. production) awkward — you'd need conditional
logic inside `OnConfiguring` itself.

`AddDbContext<TContext>` (or `AddDbContextFactory<TContext>` when you need to create contexts
outside the DI scope lifetime, e.g. in a background service) registers the context with the DI
container and lets the host configure it externally:

```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
```

`AddDbContext` registers the context with a scoped lifetime by default — one instance per DI scope
(one per HTTP request in ASP.NET Core). Your `DbContext`'s constructor takes
`DbContextOptions<TContext>` and forwards it to the base constructor; you do not also implement
`OnConfiguring` when you register this way unless you're layering on configuration the DI
registration doesn't already cover (uncommon — prefer putting everything in the `AddDbContext`
delegate).

When both `OnConfiguring` and `AddDbContext` configure the same context, `OnConfiguring` runs after
the DI-supplied options and can override them — a source of surprising behavior if you don't intend
that layering. Pick one configuration path per context.

## Design-time context creation

Tooling (migrations, `dotnet ef`) needs to construct your `DbContext` without a running DI
container. If your context's constructor requires services beyond `DbContextOptions<TContext>` that
the tooling can't resolve, implement `IDesignTimeDbContextFactory<TContext>` in the same project so
the tooling has an explicit construction path:

```csharp
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=AppDb;Trusted_Connection=True;");
        return new AppDbContext(optionsBuilder.Options);
    }
}
```
