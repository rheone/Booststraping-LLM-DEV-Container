# Naming Decoder

Every skill this tool produces gets a category prefix that identifies its primary subject.

| Type | Prefix | Covers | Examples |
| --- | --- | --- | --- |
| Language | `csharp-*` | C# language/compiler features | `csharp-generics`, `csharp-pattern-matching`, `csharp-nullable-reference-types` |
| .NET/BCL | `dotnet-*` | .NET/BCL/platform functionality | `dotnet-reflection`, `dotnet-system-text-json`, `dotnet-dependency-injection` |
| Library | `dotnet-*` | Specific third-party .NET libraries/packages | `dotnet-mediatr`, `dotnet-automapper`, `dotnet-nhibernate` |
| Pattern | `csharp-*` | Design patterns and their C# implementation | `csharp-builder-pattern`, `csharp-strategy-pattern`, `csharp-mediator-pattern` |
| Architecture | `csharp-*` | Architectural approaches as applied to C#/.NET | `csharp-vertical-slice`, `csharp-cqrs`, `csharp-ddd` |
| Convention | `csharp-*` | Coding practices/conventions | `csharp-naming`, `csharp-code-organization`, `csharp-documentation` |
| Tooling | `dotnet-*` | C#/.NET development tools | `dotnet-csharpier`, `dotnet-roslynator` |
| Testing | `dotnet-*` | Testing frameworks and .NET testing practices | `dotnet-xunit`, `dotnet-integration-testing` |

## The test

The prefix follows what the skill is *primarily describing*, not what language it happens to be
written in — every skill here is C#/.NET code either way.

- Use `csharp-<concept>` when the skill describes a programming concept or technique being applied
  in C#/.NET: a language feature, a design pattern, an architectural style, or a coding convention.
  The concept would still make sense as a heading in a general programming book; C# is just the
  lens.
- Use `dotnet-<technology>` when the skill describes a specific .NET technology or dependency: a
  BCL namespace, a specific NuGet package, a specific tool, or a specific testing framework. Naming
  the skill without the technology's own name would leave nothing to say.

## Resolving an ambiguous case

Some subjects sit on the boundary — a BCL API surface that's also deeply integrated into language
syntax, a technique that's really about one specific type. Reason it through with the test above
before defaulting either way:

- **Microsoft.Extensions.DependencyInjection** → `dotnet-dependency-injection`: the skill is about
  a specific container's API (`IServiceCollection`, lifetimes), not the general DI concept.
- **LINQ** → `dotnet-linq`: its entire surface is `System.Linq`, a BCL API; the query-syntax sugar
  is a thin layer over method calls the skill still documents as BCL operators.
- **`Span<T>`/`Memory<T>` as a performance technique** → stays `csharp-span-and-memory`: the skill
  teaches a technique (avoiding allocations, working with slices) that happens to use BCL types as
  its vocabulary, not "how to use the `Span<T>` API" as an end in itself.
- **Declaring and consuming attributes** → stays `csharp-system-attributes`: attribute syntax
  itself is a C# language mechanism; specific framework attribute sets are out of scope for that
  skill entirely (see its own scope section), not a reason to rename it `dotnet-*`.

## If you can't determine it

If, after applying the test above, you genuinely cannot tell which prefix fits — or the subject
plausibly spans both in a way these examples don't resolve — stop and ask the person commissioning
the skill which prefix and name to use. Do not guess and proceed; a wrongly named skill is a
naming-convention violation baked into a permanent file path.
