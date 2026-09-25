# Registration syntax

How a component is *exposed* (what it can be `Resolve`d as) is independent of how it's
*constructed*. `RegisterType<T>()` alone only exposes the component as its own concrete type by
default — everything below controls what services a registration answers to.

## `As<TService>()`

Exposes the registered component under a specific service type (usually an interface). Multiple
`As<T>` calls on the same registration expose the same instance/activation under several service
types simultaneously.

```csharp
builder.RegisterType<OrderService>()
    .As<IOrderService>()
    .As<IOrderValidator>();   // same OrderService instance answers both requests, per its lifetime
```

## `AsSelf()`

Additionally exposes the component as its own concrete type, alongside whatever `As<T>` calls are
also present. Useful when some callers legitimately need the concrete type (e.g. a base class
constructor test, or a framework that resolves controllers by concrete type) while others depend
on the abstraction.

```csharp
builder.RegisterType<OrderService>().As<IOrderService>().AsSelf();
// resolvable as both IOrderService and OrderService
```

## `AsImplementedInterfaces()`

Exposes the component as every interface it implements (excluding compiler-generated/marker
interfaces Autofac special-cases out, such as base interfaces from the BCL it deliberately
excludes by default). Convenient for types with exactly one meaningful interface; risky for types
that implement several interfaces for unrelated reasons, since all of them become resolvable
routes to the same instance.

```csharp
builder.RegisterType<OrderService>().AsImplementedInterfaces();
```

## No exposure call at all

`builder.RegisterType<OrderService>();` with no `As`/`AsSelf`/`AsImplementedInterfaces` still
registers the component and makes it resolvable as its own concrete type — this is Autofac's
default when no service call is chained, functionally equivalent to appending `.AsSelf()`.
Relying on this implicit behavior is legal but worth making explicit with `.AsSelf()` when
readability matters, since it's easy to misread as "not exposed at all."

## Named and keyed registrations

When multiple implementations of the same service type need to coexist and be disambiguated at
resolve time:

```csharp
builder.RegisterType<SmtpEmailSender>().Named<IEmailSender>("smtp");
builder.RegisterType<SendGridEmailSender>().Named<IEmailSender>("sendgrid");

var sender = container.ResolveNamed<IEmailSender>("sendgrid");
```

`Keyed<TService>(object key)` is the same idea with an arbitrary object key instead of a string —
useful for enum-keyed selection:

```csharp
builder.RegisterType<SmtpEmailSender>().Keyed<IEmailSender>(EmailProvider.Smtp);
var sender = container.ResolveKeyed<IEmailSender>(EmailProvider.Smtp);
```

To inject "give me the one named/keyed X" into a constructor without touching the container
directly, register a factory delegate or use the `IIndex<TKey, TService>` relationship type
Autofac auto-generates for keyed/named registrations of a common service type:

```csharp
public class NotificationRouter
{
    private readonly IIndex<EmailProvider, IEmailSender> _senders;
    public NotificationRouter(IIndex<EmailProvider, IEmailSender> senders) => _senders = senders;

    public IEmailSender Get(EmailProvider provider) => _senders[provider];
}
```

## Open generic registrations

`RegisterGeneric` (or the `.SetLifetimeScope`/service-chain form on `RegisterType(typeof(Repo<>))`)
registers an open generic type definition so Autofac can construct the closed generic on demand
for whatever type argument is requested:

```csharp
builder.RegisterGeneric(typeof(EfRepository<>)).As(typeof(IRepository<>));

// resolves EfRepository<Order> for IRepository<Order>, EfRepository<Customer> for
// IRepository<Customer>, etc., without a separate registration per closed type
var orderRepo = container.Resolve<IRepository<Order>>();
```

This is the standard way to avoid registering `IRepository<Order>` -> `EfRepository<Order>`,
`IRepository<Customer>` -> `EfRepository<Customer>`, ... one-by-one for a generic repository/
handler/validator pattern.

## `PreserveExistingDefaults()` and registration order

By default, when the same service type is registered more than once, the *last* registration wins
for a plain (non-collection) `Resolve<T>` — earlier registrations still participate in
`IEnumerable<T>` resolution (Autofac resolves "all registered implementations" for an
`IEnumerable<T>`/`IList<T>`/etc. parameter automatically), just not as the single default.
`.PreserveExistingDefaults()` flips that: it registers the component as a *non-default* choice
without overriding whatever was registered first.

```csharp
builder.RegisterType<DefaultLogger>().As<ILogger>();
builder.RegisterType<VerboseLogger>().As<ILogger>().PreserveExistingDefaults();
// Resolve<ILogger>() still returns DefaultLogger; both appear in Resolve<IEnumerable<ILogger>>()
```
