# Testing With Fake Repositories

Because consumers depend on a repository interface, not on a concrete data-access implementation,
you test them against an in-memory fake that implements the same interface with a plain in-memory
collection standing in for real storage. No real database, ORM, or network call is involved.

## Writing the fake

```csharp
public sealed class FakeCustomerRepository : ICustomerRepository
{
    private readonly List<Customer> _customers = new();
    private int _nextId = 1;

    public Customer? GetById(int id) => _customers.FirstOrDefault(c => c.Id == id);

    public IReadOnlyList<Customer> GetByRegion(string region) =>
        _customers.Where(c => c.Region == region).ToList();

    public void Add(Customer customer)
    {
        customer.Id = _nextId++;
        _customers.Add(customer);
    }
}
```

The fake reproduces the interface's observable behavior — assigning an ID on `Add`, filtering by
region on `GetByRegion` — using nothing but a `List<T>`. It contains no infrastructure, no
connection setup, and nothing that needs disposing.

## Testing a consumer against the fake

```csharp
[Fact]
public void RegisterCustomer_assigns_a_new_customer_an_id()
{
    var repository = new FakeCustomerRepository();
    var service = new CustomerRegistrationService(repository);

    var customer = service.Register(name: "Ada", region: "West");

    Assert.NotEqual(0, customer.Id);
    Assert.Equal(customer, repository.GetById(customer.Id));
}

[Fact]
public void GetActiveCustomersInRegion_returns_only_matching_customers()
{
    var repository = new FakeCustomerRepository();
    repository.Add(new Customer { Name = "Ada", Region = "West", IsActive = true });
    repository.Add(new Customer { Name = "Grace", Region = "East", IsActive = true });
    var service = new CustomerLookupService(repository);

    var result = service.GetActiveCustomersInRegion("West");

    Assert.Single(result);
    Assert.Equal("Ada", result[0].Name);
}
```

Seeding the fake directly with `Add` calls, then asserting on what the service returns, tests the
consumer's logic without needing the fake to be anything more than a correct, minimal stand-in for
the interface.

## What a fake repository test does and doesn't prove

A test built against a fake proves that the *consumer* — the service, the handler, the use case —
does the right thing given a correctly-behaving repository. It does not prove that a *real*
repository implementation actually behaves the way the fake assumes, especially for any query logic
more complex than the fake's own straightforward `List<T>` filtering can faithfully represent. This
gap matters most for repositories exposing `IQueryable<T>` or provider-specific query composition
(see [iqueryable-and-leaky-abstractions.md](iqueryable-and-leaky-abstractions.md)) — a fake backed by
`List<T>.AsQueryable()` will happily execute LINQ expressions a real provider might translate
differently or reject outright. Cover that gap with a separate, narrower set of tests that exercise
the real repository implementation against real (or realistically configured) storage, reserved for
the query logic itself rather than for every consumer that happens to use the repository.

## Fakes vs. mocking-framework substitutes

A hand-written fake, backed by a real in-memory list, tends to give more reliable test behavior for
a repository specifically because a repository's whole contract is "acts like a collection" —
letting a fake actually behave like one (assigning IDs, filtering, persisting across calls within
the test) catches consumer bugs that a substitute only recording and replaying configured call
expectations would miss entirely, such as a consumer that adds an entity and then expects to read it
back by ID in the same test.

## Common scenarios worth covering

- **Not-found paths.** Assert on what the consumer does when `GetById` returns `null` (or an empty
  result) — a fake makes this trivial to set up by simply not seeding the entity in question.
- **Duplicate/conflict detection.** If a consumer checks for an existing entity before adding a new
  one, seed the fake with a conflicting entity and assert the consumer's conflict-handling path runs
  instead of a blind `Add`.
- **Filtering and ordering logic that lives in the consumer**, not the repository — seed the fake
  with entities in an order or mix that would fail if the consumer's own filtering or sorting logic
  were wrong, even though the fake itself returns entities unsorted or in insertion order.
