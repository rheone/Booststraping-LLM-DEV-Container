# Adapting a Third-Party Library's API

The most common real-world use of the Adapter pattern is insulating an application from a
third-party library's specific API shape: its own types, its own method names, its own way of
signaling errors, and its own configuration model. You define the target interface around what
your application needs, and let the adapter absorb everything the library does differently.

## Why this matters beyond simple translation

Wrapping a third-party client directly in an adapter you own buys you three things:

- **Replaceability.** If the library changes its API in a breaking way, or you switch to a
  different library entirely, only the adapter changes — every consumer coded against your target
  interface is untouched.
- **A vocabulary that matches your domain.** A third-party client's method names and parameter
  shapes reflect that library's own model, not your application's. The target interface lets you
  name operations the way your domain thinks about them (`ISmsSender.Send`, not
  `IThirdPartyClient.EnqueueMessageV2`).
- **A narrow surface to fake in tests.** Your target interface exposes only the handful of members
  your application actually calls, instead of the library's full client surface — see
  [testing-adapters.md](testing-adapters.md) for what that buys you.

## Shape

```csharp
// Your own abstraction, named for what your application needs.
public interface IObjectStorage
{
    Task UploadAsync(string key, Stream content, CancellationToken cancellationToken);
    Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken);
}

// The adapter wraps the third-party client instance and translates its shape.
public sealed class ThirdPartyObjectStorageAdapter : IObjectStorage
{
    private readonly ThirdPartyStorageClient _client;
    private readonly string _bucketName;

    public ThirdPartyObjectStorageAdapter(ThirdPartyStorageClient client, string bucketName)
    {
        _client = client;
        _bucketName = bucketName;
    }

    public async Task UploadAsync(string key, Stream content, CancellationToken cancellationToken)
    {
        var request = new ThirdPartyPutRequest(_bucketName, key, content);
        ThirdPartyPutResponse response = await _client.PutObjectAsync(request, cancellationToken);
        if (!response.IsSuccess)
        {
            throw new ObjectStorageException(response.ErrorCode);
        }
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken)
    {
        var request = new ThirdPartyGetRequest(_bucketName, key);
        ThirdPartyGetResponse response = await _client.GetObjectAsync(request, cancellationToken);
        return response.Content;
    }
}
```

Nothing outside `ThirdPartyObjectStorageAdapter` references `ThirdPartyStorageClient`,
`ThirdPartyPutRequest`, or the library's response/error shapes. Configuration specific to the
library — the bucket name, credentials, retry policy — lives at the point where you construct the
adapter, not inside `IObjectStorage`'s contract.

## Keeping the boundary from leaking

Two disciplines keep this adapter from becoming a thin pass-through that leaks the library anyway:

- **Never return a third-party type from the target interface.** If `DownloadAsync` returned the
  library's own response object instead of a plain `Stream`, every consumer would still need to
  know that type's shape — the adapter would exist in name only.
- **Translate the library's exceptions, not just its return values.** If the library throws its own
  exception types on failure, catch them at the adapter boundary and rethrow as an exception type
  your application defines, the same way you'd translate an error-code return value. A `catch`
  block that only handles happy-path return shapes but lets library-specific exceptions propagate
  still leaks the library into every consumer's exception handling.

## One adapter per third-party surface, not one per call site

Write a single adapter class per third-party client, exposing every operation your application
needs from that client through one target interface — not a separate ad hoc adapter at each call
site. A single, well-named `IObjectStorage` implementation is one place to update when the library
changes; five inline translations scattered across the codebase are five places.
