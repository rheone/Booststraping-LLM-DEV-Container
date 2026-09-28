// TEMPLATE — session factory configuration + ASP.NET Core DI wiring, in one place.
// Every other reference file in this skill talks about interceptors, filters, batch size, and
// cache strategy as things that get "configured once at the session factory level" — this is
// the file that shows all of them wired together, since no single reference file owned this
// before and the knowledge was scattered across five of them with no canonical example.

using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using Microsoft.Extensions.DependencyInjection;
using NHibernate;
using NHibernate.Cfg;

public static class NHibernateServiceCollectionExtensions
{
    public static IServiceCollection AddNHibernateStack(this IServiceCollection services, string connectionString)
    {
        // ISessionFactory: expensive to build, thread-safe, exactly one per application.
        // Registered as a singleton — never scoped/transient, see reference/glossary.md.
        services.AddSingleton<ISessionFactory>(sp => BuildSessionFactory(connectionString));

        // ISession: one per request. Scoped lifetime is what actually implements the
        // session-per-request convention from reference/session-lifecycle.md — get this
        // registration's lifetime wrong and that whole convention silently stops applying.
        services.AddScoped(sp => sp.GetRequiredService<ISessionFactory>().OpenSession());

        // Repositories depend on the scoped ISession above, so they're naturally scoped too —
        // don't register repositories as singleton, they'd capture a session from the first
        // request forever.
        // services.AddScoped<OrderRepository>();

        return services;
    }

    private static ISessionFactory BuildSessionFactory(string connectionString)
    {
        return Fluently.Configure()
            .Database(MsSqlConfiguration.MsSql2012.ConnectionString(connectionString)) // adjust dialect — see reference/dialect-notes.md
            .Mappings(m => m
                .FluentMappings.AddFromAssemblyOf<{SomeEntityMap}>()
                // Conventions: see reference/fluent-convention-api.md — enforces the mechanical
                // rules from reference/mapping-conventions.md (ID generation, table/FK naming,
                // default batch size) automatically instead of relying on every mapping author
                // to remember them by hand.
                .Conventions.AddFromAssemblyOf<HiLoIdConvention>())

            // Interceptor: see reference/filters-and-interceptors.md — audit columns, auto-timestamps.
            // Registered once here, applies to every session from this factory uniformly.
            .ExposeConfiguration(cfg => cfg.SetInterceptor(new AuditInterceptor()))

            // Batch size: see reference/mapping-conventions.md and reference/bulk-and-stateless.md —
            // reduces round trips for inserts/updates. Confirm identifier generation strategy is
            // batching-compatible (HiLo is; Identity mostly defeats this) before relying on it.
            .ExposeConfiguration(cfg => cfg.SetProperty(Environment.BatchSize, "20"))

            // Second-level cache: OFF by default, and it should stay off globally — enable per-entity
            // in each entity's own mapping (Cache.ReadWrite() etc.), not blanket-enabled here. See
            // reference/caching-and-concurrency.md for which entities are actually safe to cache.
            // .ExposeConfiguration(cfg => cfg.SetProperty(Environment.UseSecondLevelCache, "true"))
            // .ExposeConfiguration(cfg => cfg.SetProperty(Environment.CacheProvider, typeof(YourCacheProvider).AssemblyQualifiedName))

            // SQL logging: see reference/sql-diagnostics.md — scope this to Development via
            // standard logging configuration (appsettings.Development.json), not a compile-time flag
            // baked into this method, so production doesn't accidentally ship with it on.
            // .ExposeConfiguration(cfg => cfg.SetProperty(Environment.ShowSql, "true"))

            .BuildSessionFactory();
    }
}

// Filter enablement: FilterDefinition is registered above via the FluentMappings scan (each
// entity's HasFilter(...) call), but EnableFilter(...) must be called PER SESSION, not at the
// factory level — see reference/filters-and-interceptors.md. Do this in the same place the
// scoped ISession gets created, e.g. via a small decorator or a piece of request middleware:
//
// services.AddScoped(sp =>
// {
//     var session = sp.GetRequiredService<ISessionFactory>().OpenSession();
//     session.EnableFilter("SoftDelete");
//     return session;
// });
