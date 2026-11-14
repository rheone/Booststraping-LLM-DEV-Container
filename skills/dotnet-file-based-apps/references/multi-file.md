# Multi-file apps

By default a file-based app compiles only its **entry file**. Other `.cs` files in the folder are ignored. To use more files, choose by what the extra code is:

| The extra code is… | Use | Minimum SDK band |
| --- | --- | --- |
| Helper types for this one app | `#:include` | 10.0.300 |
| A library another file-based app also uses | `#:ref` | 10.0.300 with flag; 11 without |
| An existing `.csproj` | `#:project` | 10.0.100 |
| A compiled `.dll` | `#:include path.dll` | 10.0.400 |

Check the band with `dotnet --version` before you write any of these.

## The shebang rule

An entry file that uses `#:include` or `#:ref` starts with a shebang, even on Windows. The `#!` line marks it as the entry point for the IDE and for analyzer CA2266, which warns when it's missing. It doesn't affect the build.

```csharp
#!/usr/bin/env -S dotnet --
#:include helpers.cs
```

## `#:include` and `#:exclude`

```csharp
#!/usr/bin/env -S dotnet --
#:include helpers.cs
#:include models/*.cs
#:exclude models/Legacy.cs

Console.WriteLine(Helpers.Greeting(new Customer("Ada")));
```

```csharp
// helpers.cs: types and members only
static class Helpers
{
    public static string Greeting(Customer c) => $"Hello, {c.Name}";
}
```

- Included `.cs` files add types, members and namespaces. **They cannot contain top-level statements.** Only the entry file can.
- Paths are relative to the file that holds the directive. Globs work (`shared/**/*.cs`), but **any glob turns off the build cache**, so every run rebuilds. List files by name when the set is small.
- Item type follows the extension: `.cs` → Compile, `.json` → None, `.resx` → EmbeddedResource, `.razor` → Content, `.dll` → Reference.
- An included file can carry its own `#:package` and other directives. 10.0.4xx and 11 apply them. On 10.0.3xx they need `#:property ExperimentalFileBasedProgramEnableTransitiveDirectives=true`.
- On 10.0.1xx–3xx, the same directive in two files is a duplicate error. Put shared directives in the entry file only.
- Keep the glob scope narrow. `#:include **/*.cs` in a large folder walks all of it.
- `#:exclude` has the same syntax and removes items an include added.

## `#:ref`: a file as a library

`#:ref` compiles another `.cs` file as its own virtual library project and references it.

```csharp
// lib.cs
#:property OutputType=Library
public static class Lib
{
    public static string Hi() => "from lib";
}
```

```csharp
#!/usr/bin/env -S dotnet --
#:property ExperimentalFileBasedProgramEnableRefDirective=true
#:ref lib.cs

Console.WriteLine(Lib.Hi());
```

- **SDK 11:** no flag. Leave out the `ExperimentalFileBasedProgramEnableRefDirective` line.
- **10.0.3xx/4xx:** the flag is required. The `#:property` line above sets it, or the environment variable of the same name does. Without it, the build fails with "This is an experimental feature, set MSBuild property 'ExperimentalFileBasedProgramEnableRefDirective' to 'true'".
- **10.0.1xx/2xx:** `#:ref` is an unrecognized directive. Use `#:project` to a classlib, or convert to a project.
- The referenced file needs `#:property OutputType=Library`, because it is an `Exe` by default.
- The referencing file cannot see its `internal` members. Make the API `public`.

## `#:project`

```csharp
#:project ../Shared/Shared.csproj
```

This works on every band. `dotnet reference add ../Shared/Shared.csproj --file app.cs` writes the line for you (tested on 10.0.404 and 11.0.100).

## Compiled DLLs

```csharp
#!/usr/bin/env -S dotnet --
#:include lib/Vendor.dll
```

Tested on 10.0.404 and 11.0.100. Microsoft Learn lists it as .NET 11. The 10.0.3xx source has no `.dll` mapping, so on 10.0.3xx use `#:project` or a package instead.

## When multi-file is not enough

Convert to a project when the code needs tests, a second entry point, or project features that directives cannot express. See [convert-to-project.md](convert-to-project.md).
