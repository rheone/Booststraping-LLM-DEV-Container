# Extending With New Repositories

Adding data access for a new aggregate should mean introducing that aggregate's repository — never
modifying an existing repository interface, an existing implementation, or a consumer that depends
on a different aggregate's repository.

## Adding a repository for a new aggregate

1. Decide whether the new aggregate fits the existing generic `IRepository<T>` as-is, needs a
   specific interface of its own, or needs a specific interface built on the shared generic base —
   see [generic-vs-specific-repositories.md](generic-vs-specific-repositories.md).
2. Implement the interface against the same data-access technology existing repositories use (or a
   different one, if this aggregate's storage genuinely differs — the pattern doesn't require every
   aggregate to share a backing technology).
3. Register or construct the new repository wherever the application wires up its dependencies, and
   inject it into whatever consumer needs it. No existing repository interface, implementation, or
   consumer changes as a result.

```csharp
// Existing.
public interface ICustomerRepository { /* ... */ }
public sealed class CustomerRepository : ICustomerRepository { /* ... */ }

// New aggregate, new repository, nothing existing changes.
public interface IShipmentRepository
{
    Shipment? GetById(int id);
    IReadOnlyList<Shipment> GetPending();
    void Add(Shipment shipment);
}

public sealed class ShipmentRepository : IShipmentRepository
{
    public Shipment? GetById(int id) => /* ... */ null;
    public IReadOnlyList<Shipment> GetPending() => /* ... */ new List<Shipment>();
    public void Add(Shipment shipment) { /* ... */ }
}
```

## Adding a new query to an existing aggregate's repository

Adding a new query method to a specific repository interface (`GetOverdueInvoices` alongside an
existing `IInvoiceRepository`) is a smaller, more localized change than adding a whole new
repository — but it's still a breaking change to that one interface, since every implementation of
it (a real one, a fake used in tests) now has to add the new method. Two ways to keep this from
forcing every implementation to change immediately:

- Give the new method a default implementation on the interface (a C# 8.0+ default interface
  member) expressed in terms of existing members, when that's possible — a `GetOverdueInvoices`
  default implemented via an existing `GetAll` plus a filter, for instance — so existing
  implementations pick it up automatically and can override it later for efficiency.
- If a specification-based approach is already in place (see
  [specification-based-queries.md](specification-based-queries.md)), add the new query as a new
  specification class instead of a new interface method — the repository's `Find(ISpecification<T>)`
  method needs no change at all.

## Keeping a fake repository in sync

Any in-memory/fake implementation used in tests (see
[testing-with-fake-repositories.md](testing-with-fake-repositories.md)) implements the same
interface a real repository does, so adding a member to that interface means updating the fake too.
Treat the fake as a first-class implementation to keep current, not an afterthought — a fake that
silently falls behind the real interface either fails to compile (loud, and easy to fix immediately)
or, worse, implements the new member with behavior that doesn't match what the real implementation
actually does, which surfaces as a test that passes against the fake and fails against real storage.
