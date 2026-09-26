# Generic Adapter

When a codebase adapts many unrelated source/target pairs through the same shape — one input,
translated to one output, with no other behavior — a generic adapter interface removes the
per-pair boilerplate of declaring a bespoke interface for each one.

## The interface

```csharp
public interface IAdapter<in TSource, out TTarget>
{
    TTarget Adapt(TSource source);
}
```

`TSource` is contravariant and `TTarget` is covariant, matching how the interface is used: you only
ever pass a `TSource` in and take a `TTarget` out, never the reverse.

## Implementing it

```csharp
public sealed record LegacyCustomerRecord(string FullName, string EmailAddr);
public sealed record Customer(string Name, string Email);

public sealed class LegacyCustomerAdapter : IAdapter<LegacyCustomerRecord, Customer>
{
    public Customer Adapt(LegacyCustomerRecord source) =>
        new Customer(source.FullName, source.EmailAddr);
}
```

Consuming code depends on `IAdapter<LegacyCustomerRecord, Customer>` rather than on
`LegacyCustomerAdapter` directly, so a different translation can be substituted — a fake in a test,
or an alternate mapping for a different legacy source — without changing the call site:

```csharp
public sealed class CustomerImportJob
{
    private readonly IAdapter<LegacyCustomerRecord, Customer> _adapter;

    public CustomerImportJob(IAdapter<LegacyCustomerRecord, Customer> adapter) => _adapter = adapter;

    public IEnumerable<Customer> Import(IEnumerable<LegacyCustomerRecord> records) =>
        records.Select(_adapter.Adapt);
}
```

## When the generic form earns its keep

Reach for `IAdapter<TSource, TTarget>` when:

- The codebase has several adapters that are purely a one-in, one-out translation, and you want a
  single, swappable abstraction for "translate an X into a Y" that generic infrastructure (a
  collection of adapters resolved by type, a batch-translation helper) can operate over uniformly.
- The translation itself has no side effects and no dependencies beyond the value being adapted —
  the moment an adapter needs to call out to an adaptee instance to do its translation (the common
  case for a third-party wrapper), model it as an ordinary object adapter instead and skip the
  generic interface; forcing that shape into `IAdapter<TSource, TTarget>` just adds a layer of
  indirection with nothing to swap.

## Two-way generic adapters

Some scenarios need translation in both directions. Rather than overloading `IAdapter<TSource,
TTarget>` with a second method, declare a distinct interface so each direction stays single-purpose
and independently swappable:

```csharp
public interface ITwoWayAdapter<TLeft, TRight>
{
    TRight AdaptToRight(TLeft left);
    TLeft AdaptToLeft(TRight right);
}
```
