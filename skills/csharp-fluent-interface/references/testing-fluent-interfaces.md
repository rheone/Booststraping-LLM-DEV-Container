# Testing Fluent Interfaces

Testing a fluent API means asserting on the state the chain produced, not on the mechanics of the
chain itself — the intermediate return values a chain passes through are an implementation detail of
*how* the API composes, not something a test should depend on.

## Assert on final state, not on intermediate links

```csharp
[Fact]
public void HttpRequestConfig_Test_ChainAppliesEveryConfiguredValue()
{
    var config = new HttpRequestConfig()
        .WithMethod("POST")
        .WithUri(new Uri("https://api.example.com/orders"))
        .WithHeader("Authorization", "Bearer token");

    Assert.Equal("POST", config.Method);
    Assert.Equal("https://api.example.com/orders", config.Uri.ToString());
    Assert.Equal("Bearer token", config.Headers["Authorization"]);
}
```

The test calls the whole chain as one expression, exactly as production code would, then asserts on
the resulting object's observable members — it never checks what `WithMethod` returned on its own,
because for a mutable, self-returning chain that's always `this`, and asserting on it would just be
re-testing the language's own reference semantics.

## Testing a mutable chain: verify mutation, not just the final read

```csharp
[Fact]
public void HttpRequestConfig_Test_ReturnsSameInstanceThroughoutTheChain()
{
    var config = new HttpRequestConfig();
    var afterFirstCall = config.WithMethod("POST");

    Assert.Same(config, afterFirstCall);
}
```

Reach for `Assert.Same` only when the mutable-vs-immutable distinction is itself the behavior under
test (verifying a bug fix where a method accidentally started returning a new instance instead of
`this`, for example) — for ordinary feature tests, asserting on final state alone is enough and
doesn't couple the test to which chain shape the implementation happens to use.

## Testing an immutable chain: verify the original wasn't touched

```csharp
[Fact]
public void ShippingOptions_Test_RequireSignatureLeavesOriginalUnchanged()
{
    var original = new ShippingOptions(SignatureRequired: false, Insured: false, DeclaredValue: 0m);

    var upgraded = original.RequireSignature();

    Assert.False(original.SignatureRequired);
    Assert.True(upgraded.SignatureRequired);
}
```

An immutable chain's defining guarantee — the receiver is untouched — is exactly the thing worth a
dedicated assertion for, since a regression that accidentally starts mutating in place would
otherwise only surface as a much harder to diagnose bug somewhere a shared "base" value gets reused.

## Testing a staged fluent API

A staged API's ordering guarantee is enforced by the compiler, not at run time — there is no legal
call sequence to construct that skips a stage, so there is nothing to unit-test about the ordering
itself. What's worth testing is that each stage's terminal call produces correct output for the
values passed through the stages that did run:

```csharp
[Fact]
public void ClientBuilder_Test_ProducesClientWithConfiguredHostAndKey()
{
    var client = ClientBuilder.Create()
        .ConnectTo("api.example.com")
        .AuthenticateWith("test-key")
        .Build();

    Assert.Equal("api.example.com", client.Host);
    Assert.Equal("test-key", client.ApiKey);
}
```

## Testing an extension-method fluent call

```csharp
[Fact]
public void AppendLineIf_Test_AppendsOnlyWhenConditionIsTrue()
{
    var whenTrue = new StringBuilder().AppendLineIf(true, "included");
    var whenFalse = new StringBuilder().AppendLineIf(false, "excluded");

    Assert.Contains("included", whenTrue.ToString());
    Assert.DoesNotContain("excluded", whenFalse.ToString());
}
```

An extension method is a static method underneath the fluent call syntax — test it exactly like any
other static method, calling it through the chain syntax (to match how production code actually
invokes it) and asserting on the extended instance's resulting state, the same as any other
mutable-chain link.
