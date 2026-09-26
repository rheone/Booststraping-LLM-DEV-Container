# Scope and Claim Configuration

Scopes gate what a token is allowed to request; claims are the actual data a token carries once
issued. OpenIddict distinguishes both from resources, and controls which claims a given scope
exposes into the access token versus the ID token.

## Registering scopes server-wide

`RegisterScopes(...)` on the server options declares the scope names the server recognizes at all —
a scope not registered here is rejected regardless of what any client's permissions say:

```csharp
options.RegisterScopes("api", "profile", "email", Scopes.OfflineAccess);
```

`Scopes.OfflineAccess`, `Scopes.Profile`, `Scopes.Email`, and other standard OpenID Connect scope
names live in `OpenIddict.Abstractions.Scopes` as constants — using them instead of hand-typed
strings avoids a typo silently creating an unrecognized custom scope.

## Granting scope permissions per client application

A scope registered server-wide is still not automatically usable by every client — each application
needs the corresponding `Permissions.Scopes.Prefix + <name>` permission:

```csharp
Permissions =
{
    Permissions.Scopes.Prefix + "api",
    Permissions.Scopes.Profile,
    Permissions.Scopes.Email,
}
```

A client requesting a scope it isn't permitted rejects at the authorization/token endpoint even
though the scope itself is registered and enabled server-wide — this two-layer gate (server-wide
registration, then per-client permission) is deliberate: it lets you register a scope for future use
without immediately exposing it to every existing client.

## Registering a resource-scoped scope entity (advanced)

For finer-grained control — restricting a scope to specific resource servers via the `aud` (audience)
claim — register the scope as a full entity through `IOpenIddictScopeManager` rather than the
lightweight `RegisterScopes` call, and associate specific resources with it:

```csharp
await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
{
    Name = "api",
    Resources = { "resource-server-1" },
});
```

A validating resource server that calls `AddAudiences("resource-server-1")` (see
[validation-configuration.md](validation-configuration.md)) only accepts tokens whose audience
includes that name — this is how a scope maps to "which API can this token actually call."

## Attaching claims to the identity before issuance

Claims placed on the `ClaimsIdentity` before calling `Results.SignIn(...)` (see
[authorization-code-flow.md](authorization-code-flow.md)) become part of the issued token — but only
if you also declare which destinations (`access_token`, `id_token`) each claim should be serialized
into:

```csharp
identity.SetClaim(Claims.Subject, userId);
identity.SetClaim(Claims.Email, userEmail);
identity.SetDestinations(claim => claim.Type switch
{
    Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],
    Claims.Email when identity.HasScope(Scopes.Email) => [Destinations.IdentityToken],
    _ => [],
});
```

A claim with no destination set is attached to the identity in memory but never actually serialized
into any issued token — this is the most common cause of "I set the claim but it's not in the JWT."
`identity.HasScope(...)` gates a claim's presence on whether the caller actually requested the
corresponding scope, which is the standard OpenID Connect behavior for claims like `email` and
`profile`.
