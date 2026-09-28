# Mutable, Self-Returning Chains

The most common fluent shape: every chained method mutates the receiver's own state and returns
`this`, so the chain is really one statement operating on a single shared instance from start to
finish.

## Basic: a configuration object built up in place

```csharp
public sealed class HttpRequestConfig
{
    private readonly Dictionary<string, string> _headers = new();
    private string _method = "GET";
    private Uri? _uri;

    public HttpRequestConfig WithMethod(string method) { _method = method; return this; }
    public HttpRequestConfig WithUri(Uri uri) { _uri = uri; return this; }
    public HttpRequestConfig WithHeader(string name, string value) { _headers[name] = value; return this; }
}
```

```csharp
var config = new HttpRequestConfig()
    .WithMethod("POST")
    .WithUri(new Uri("https://api.example.com/orders"))
    .WithHeader("Authorization", $"Bearer {token}");
```

Each call writes directly into `_headers`, `_method`, or `_uri` on the one instance the chain
started from, then hands that same instance back. There is only ever one `HttpRequestConfig` in
play — `config` and the intermediate return values of every `With*` call are the same reference.

## Reuse: the same instance, called again later

```csharp
var config = new HttpRequestConfig().WithMethod("GET");

config.WithHeader("Authorization", $"Bearer {tokenA}");
SendRequest(config);

config.WithHeader("Authorization", $"Bearer {tokenB}");   // overwrites the previous header
SendRequest(config);
```

Because state lives on the shared instance, calling a `With*` method again later changes it for
every reference still holding that instance — useful when a caller deliberately wants to adjust and
resend the same request, a trap when two callers each assumed they had their own private copy after
one handed the config to the other.

## Thread-safety

Nothing about `return this;` implies synchronization. Two threads calling `With*` methods on the
same instance concurrently race on the same private fields exactly as they would on any other
mutable object with no lock — a fluent chain is not, by itself, a thread-safety boundary. Confine a
mutable, self-returning chain to a single thread's local construction sequence, or add explicit
locking around each mutator if concurrent configuration genuinely needs to happen.

## Requirements and restrictions

- Every chained method's return type must be (or be assignable to) the type the next call needs —
  for a self-returning chain this means literally returning `this`, not a copy, not `void`.
- A chained method that forgets to `return this;` and instead returns `void` breaks the chain at
  that exact call, forcing every method after it back into standalone statements — a compile error,
  not a silent bug, since the next `.` in the chain has nothing to attach to.
