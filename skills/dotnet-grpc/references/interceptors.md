# Interceptors

An interceptor wraps every call made through a channel (client-side) or handled by a service
(server-side) with cross-cutting logic — logging, authentication header injection, retry policy,
metrics — without touching each individual service method.

## Writing an interceptor

Derive from `Grpc.Core.Interceptors.Interceptor` and override only the method(s) matching the call
shapes you need to intercept (unary, server-streaming, client-streaming, or bidirectional — each has
its own client-side and server-side override):

```csharp
public sealed class LoggingInterceptor(ILogger<LoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        logger.LogInformation("Handling {Method}", context.Method);
        try
        {
            return await continuation(request, context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling {Method}", context.Method);
            throw;
        }
    }
}
```

`continuation` is the next interceptor in the pipeline, or the actual service method once the chain
is exhausted — every override must call it (unless deliberately short-circuiting the call, e.g. to
reject it early) or the request never reaches the real handler.

## Registering a server interceptor

```csharp
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<LoggingInterceptor>();
});
```

Global interceptors registered this way apply to every service; `MapGrpcService<T>()` also accepts
per-service interceptor configuration when only one service needs a particular interceptor.

## Writing a client interceptor

The client-side override names follow the same call-shape pattern (`BlockingUnaryCall`,
`AsyncUnaryCall`, `AsyncServerStreamingCall`, etc.). A common use is attaching an auth header to
every outgoing call:

```csharp
public sealed class AuthHeaderInterceptor(Func<string> getToken) : Interceptor
{
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var headers = context.Options.Headers ?? [];
        headers.Add("Authorization", $"Bearer {getToken()}");
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, context.Options.WithHeaders(headers));
        return continuation(request, newContext);
    }
}
```

## Registering a client interceptor

```csharp
var channel = GrpcChannel.ForAddress("https://localhost:5001");
var invoker = channel.Intercept(new AuthHeaderInterceptor(() => currentToken));
var client = new Greeter.GreeterClient(invoker);
```

or, with `AddGrpcClient`, via `.AddInterceptor<AuthHeaderInterceptor>()` chained onto the
registration — either way, the interceptor wraps the `CallInvoker` the generated client is built on,
not the channel's transport itself, so it applies uniformly regardless of which RPC shape is called
through it.
