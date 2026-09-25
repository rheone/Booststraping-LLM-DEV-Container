# ASP.NET Core integration

The `Autofac.Extensions.DependencyInjection` package replaces ASP.NET Core's built-in
`Microsoft.Extensions.DependencyInjection` container with Autofac as the actual service provider,
while still letting framework and third-party libraries register themselves the normal
`IServiceCollection` way — those registrations get bridged into Autofac automatically.

## Wiring it up: `UseServiceProviderFactory` + `ConfigureContainer`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());

builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule<OrderingModule>();
    containerBuilder.RegisterType<OrderService>().As<IOrderService>().InstancePerLifetimeScope();
});

builder.Services.AddControllers();   // still fine — bridged into the Autofac container

var app = builder.Build();
```

`UseServiceProviderFactory(new AutofacServiceProviderFactory())` tells the generic host to build
its root service provider using Autofac instead of the default MEDI container.
`ConfigureContainer<ContainerBuilder>` is where Autofac-specific registrations
(modules, named/keyed registrations, decorators, open generics — anything the plain
`IServiceCollection` API can't express) go; everything registered through `builder.Services` (the
ordinary ASP.NET Core registration surface used by framework code and most NuGet packages) is
still honored, translated into Autofac registrations automatically.

## Request-scoped lifetime

ASP.NET Core begins one child `ILifetimeScope` per HTTP request automatically once Autofac is the
provider. Any component registered `InstancePerLifetimeScope()` behaves as "one instance per HTTP
request" without any extra configuration — there's no need to reach for a separate
"InstancePerRequest" API in this hosting model (see `references/lifetime-scopes.md`).

## Resolving outside constructor injection

Inside a request pipeline, `HttpContext.RequestServices` (or, more idiomatically,
constructor-injecting `IServiceProvider`/the specific dependency itself into a controller/minimal
API handler) resolves from the current request's Autofac scope — same as with the default
container, since Autofac implements `IServiceProvider` on every scope it hands to the framework.
Reaching for `HttpContext.RequestServices.GetService` directly, rather than declaring the
dependency as a constructor/handler parameter, is a Service Locator anti-pattern regardless of
which container is behind it — see `references/common-pitfalls.md`.

## Minimal APIs and `Autofac.Extensions.DependencyInjection`

The bridge works the same way with the minimal hosting model (`WebApplication.CreateBuilder`) shown
above — there is no separate "minimal API" variant of the integration; `UseServiceProviderFactory`
and `ConfigureContainer` are host-builder-level, independent of whether MVC controllers or minimal
API endpoint delegates are used on top.
