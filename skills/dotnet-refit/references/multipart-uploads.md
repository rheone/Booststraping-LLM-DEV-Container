# Multipart Uploads

Mark the method `[Multipart]` and use one of Refit's part wrapper types for each file/form field.
`[Multipart]` changes the request body to `multipart/form-data`; it is not combined with `[Body]`.

## Uploading a stream or byte array

```csharp
public interface IUploadApi
{
    [Multipart]
    [Post("/photos")]
    Task<UploadResult> UploadPhoto(
        [AliasAs("avatar")] StreamPart avatar,
        string caption);
}
```

```csharp
await using var fileStream = File.OpenRead(path);
var part = new StreamPart(fileStream, "avatar.jpg", "image/jpeg");
await uploadApi.UploadPhoto(part, "Profile picture");
```

- `StreamPart` — wraps any `Stream`; use when the content isn't already fully materialized as
  bytes (large files, a stream from another I/O source).
- `ByteArrayPart` — wraps a `byte[]` already in memory.
- `FileInfoPart` — wraps a `FileInfo`, letting Refit open and stream the file itself.

`[AliasAs("avatar")]` sets the multipart field name sent on the wire when it needs to differ from
the C# parameter name — required whenever the API's field name isn't a legal C# identifier or
needs to match a specific casing.

## Mixing files and ordinary form fields

Any non-part parameter in a `[Multipart]` method becomes an ordinary form field alongside the file
parts, as `caption` does above — no extra attribute needed for plain scalar fields.

## Uploading multiple files

Accept an `IEnumerable<StreamPart>` (or the byte-array/file-info equivalents) for a field that
repeats:

```csharp
[Multipart]
[Post("/photos/batch")]
Task<UploadResult> UploadPhotos(IEnumerable<StreamPart> photos);
```

## Setting the multipart boundary or part content type explicitly

`StreamPart`, `ByteArrayPart`, and `FileInfoPart` constructors all accept an explicit content-type
string as their last argument — set it explicitly whenever the file extension alone wouldn't let
the server infer the right MIME type (a stream with no filename, an uncommon format).
