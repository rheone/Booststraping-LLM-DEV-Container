# Staged Fluent APIs and Call Order

A staged fluent API returns a *different* type from each phase of the chain instead of the same
type throughout, so that only the methods legal at the current stage are visible to IntelliSense and
the compiler — call order becomes a type-checked property of the API surface, not a runtime
convention a caller has to remember.

## Basic: a two-stage chain that can't skip its first step

```csharp
public interface IConnectionStage
{
    IAuthenticationStage ConnectTo(string host);
}

public interface IAuthenticationStage
{
    IReadyStage AuthenticateWith(string apiKey);
}

public interface IReadyStage
{
    Client Build();
}

public sealed class ClientBuilder : IConnectionStage, IAuthenticationStage, IReadyStage
{
    private string _host = "";
    private string _apiKey = "";

    public static IConnectionStage Create() => new ClientBuilder();

    public IAuthenticationStage ConnectTo(string host) { _host = host; return this; }
    public IReadyStage AuthenticateWith(string apiKey) { _apiKey = apiKey; return this; }
    public Client Build() => new Client(_host, _apiKey);
}
```

```csharp
Client client = ClientBuilder.Create()
    .ConnectTo("api.example.com")
    .AuthenticateWith(apiKey)
    .Build();

// ClientBuilder.Create().AuthenticateWith(apiKey) — doesn't compile:
// IConnectionStage has no AuthenticateWith member.
```

One class (`ClientBuilder`) implements every stage interface, but each interface only exposes the
methods legal at that point in the sequence. `Create()` returns `IConnectionStage`, not
`ClientBuilder`, so the caller's static type only ever sees the current stage's members — the
concrete class's full method set exists, but nothing outside this file can see past the interface it
was handed.

## Advanced: branching stages for optional paths

```csharp
public interface IPaymentStage
{
    IReadyStage PayWithCard(string cardToken);
    IReadyStage PayWithInvoice(string accountId);
}
```

A stage interface can expose more than one method when several different next steps are equally
valid from that point — the constraint being enforced is "at least one of these," not "exactly this
one method," and both `PayWithCard` and `PayWithInvoice` return the same next-stage interface,
converging the branches back into a single path for whatever comes after.

## Requirements and restrictions

- Every stage interface needs its *own* type — reusing one interface across two stages defeats the
  purpose, since it would expose the same members at both points.
- The concrete class typically implements every stage interface explicitly (or implicitly, if no
  member name collides across stages) and exposes itself to callers only through the narrowest
  current-stage interface, never as the concrete type directly — a factory method or static entry
  point returning the first stage's interface is the usual way in.
- This only prevents *ordering* mistakes visible in the type system; it doesn't replace runtime
  validation for constraints a stage boundary can't express (a numeric range, a cross-field rule
  once every stage's data is already collected) — that validation still belongs in the terminal
  operation, as in any other fluent chain.
