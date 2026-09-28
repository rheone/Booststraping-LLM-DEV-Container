# Naming and Chain Readability

A fluent chain's readability comes almost entirely from its method names reading like a sentence at
the call site — get the naming wrong and a chain that type-checks perfectly still reads as noise.

## Naming conventions by call shape

- **Setting a value**: `With<Noun>` (`WithTitle`, `WithTimeout`) — reads naturally as "constructed
  with this."
- **Toggling a flag on**: a bare, adjective-shaped verb naming the state directly (`IncludeSummary`,
  `RequireSignature`) rather than a generic `WithFlag(true)` — the boolean disappears into the
  method's own name, so a reader never has to chase down what `true` means at that call site.
- **Adding to a collection**: `Add<Noun>` or `<Noun>` as a plural-feeling verb (`AddHeader`,
  `WithLine`) — distinguish from `With<Noun>` (replaces a single value) so a reader can tell "sets
  the one value" from "appends another item" without reading the implementation.
- **Conditional or connecting words**: `And`, `Then`, `Having` as a chain grows long enough that
  bare concatenation reads ambiguously (`.Where(...).And(...)` versus two `.Where(...)` calls back
  to back that might be read as OR'd instead of AND'd) — reach for a connecting word only when the
  bare chain is genuinely ambiguous, not as decoration on every call.

## Avoiding the ambiguous boolean parameter

```csharp
// Reads fine at the declaration, ambiguous at every call site:
public QueryFilter WithSorting(bool descending) { /* ... */ return this; }

new QueryFilter().WithSorting(true);   // true — what, exactly?
```

```csharp
// The call site names the actual choice:
public QueryFilter SortDescending() { /* ... */ return this; }
public QueryFilter SortAscending() { /* ... */ return this; }

new QueryFilter().SortDescending();
```

A bare `bool` parameter on a fluent method pushes the reader back to the method's declaration to
learn what `true` means; two differently-named methods (or an enum parameter, for more than two
states) keep the meaning at the call site, which is the entire value proposition of writing the API
fluently in the first place.

## When a chain has grown too long to stay fluent

```csharp
var order = new OrderBuilder()
    .WithCustomer(customer)
    .WithLine(item1).WithLine(item2).WithLine(item3).WithLine(item4).WithLine(item5)
    .WithShippingMethod(method)
    .WithDiscount(discount)
    .Build();
```

A chain accumulating a variable-length collection reads worse the more items pile onto one line —
prefer an explicit loop or a collection-accepting overload once the item count isn't fixed at the
call site:

```csharp
var builder = new OrderBuilder().WithCustomer(customer);
foreach (var item in items)
{
    builder.WithLine(item);
}
var order = builder
    .WithShippingMethod(method)
    .WithDiscount(discount)
    .Build();
```

The loop handles the variable-length part explicitly; the fluent chain stays for the fixed-shape
configuration around it. A fluent API earns its readability from a short, fixed sequence of
meaningfully named calls — once part of the chain is really "however many of these there happen to
be," ordinary iteration communicates that better than a chain stretched across an unpredictable
number of repeated calls.

## One call per line, once a chain exceeds two or three calls

```csharp
var config = new HttpRequestConfig().WithMethod("POST").WithUri(uri).WithHeader("Authorization", token).WithHeader("Accept", "application/json");
```

```csharp
var config = new HttpRequestConfig()
    .WithMethod("POST")
    .WithUri(uri)
    .WithHeader("Authorization", token)
    .WithHeader("Accept", "application/json");
```

The one-line version forces a reader to scan horizontally past the point most editors wrap or
truncate; breaking one call per line makes each step of the chain a distinct, skimmable unit and
keeps a diff that adds or removes one call to a one-line change instead of a reflow of the entire
expression.
