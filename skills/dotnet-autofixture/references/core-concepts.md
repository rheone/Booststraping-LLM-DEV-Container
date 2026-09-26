# Core concepts

## What AutoFixture generates for you

AutoFixture builds test data by reflecting over a type's constructor and settable members, then
recursively generating values for each parameter/property — a primitive gets a deterministic-looking
but arbitrary value (a string embeds the parameter name plus a GUID fragment, a number is
incremented from a counter), and a complex type is constructed the same way, all the way down. The
point is removing the "Arrange" boilerplate of hand-building object graphs whose exact values don't
matter to the behavior under test.

## `Fixture`, `Create<T>`, `CreateMany<T>`

```csharp
var fixture = new Fixture();

var customer = fixture.Create<Customer>();

IEnumerable<Order> orders = fixture.CreateMany<Order>();      // a small default count
IEnumerable<Order> tenOrders = fixture.CreateMany<Order>(10); // an explicit count
```

- `new Fixture()` is cheap; a fresh instance per test (or per test class, reused across tests within
  it) is the normal pattern — a `Fixture` carries mutable configuration (see
  `references/customizations.md`), so sharing one across unrelated tests risks one test's
  customization leaking into another's.
- `Create<T>()` returns a fully populated `T`: every constructor parameter and every writable
  public property gets a generated value, recursively, unless a customization says otherwise.
- `CreateMany<T>()` without an explicit count returns a small, unspecified-but-consistent number of
  items — treat the exact count as an implementation detail and pass an explicit count whenever a
  test's assertions depend on how many items exist.

## Anonymous values for primitives

`Create<T>()` isn't limited to complex types — it works for a bare `int`, `string`, `DateTime`, or
any other primitive, useful when a test needs "some value of this type" without caring which:

```csharp
var orderId = fixture.Create<int>();
var customerName = fixture.Create<string>();
```

A generated `string` embeds the calling parameter or variable's inferred name when available,
making failure output more legible than a bare GUID would be (e.g. `"customerName2f4a..."` rather
than an opaque token).

## `Build<T>()` for one-off overrides

`Build<T>()` starts a fluent, per-call composer for constructing a single object with specific
members pinned to explicit values while everything else is still auto-generated:

```csharp
var vipCustomer = fixture.Build<Customer>()
    .With(c => c.Tier, CustomerTier.Vip)
    .Without(c => c.SuspendedAt)
    .Create();
```

- `.With(expression, value)` pins one member to an explicit value.
- `.Without(expression)` leaves a member at its type's default rather than auto-generating it —
  useful for a nullable reference/value member a specific test needs to stay unset.
- `.Create()` materializes the object; nothing is built until this call.

Reach for `Build<T>()` when a single test needs one or two members pinned for that test alone; reach
for a customization (`references/customizations.md`) when many tests across a suite need the same
override applied consistently.

## Why this differs from hand-built test data

Hand-writing every test's object graph makes the *irrelevant* setup as visually prominent as the
values a test actually asserts on, and it silently drifts out of sync as a type gains new required
members — a hand-built `new Customer(...)` call needs updating at every call site when the
constructor changes, where `fixture.Create<Customer>()` adapts automatically. The tradeoff is
`references/pitfalls.md`'s subject: values a test doesn't pin explicitly are genuinely arbitrary,
which can hide an assertion that was accidentally checking a specific generated value instead of a
real invariant.
