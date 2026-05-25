# split-type-to-partials Reference

## Member classification

| Category | Rule | Destination |
|---|---|---|
| Interface implementation | Method/property whose signature matches an interface member in the type's interface list | `{Type}.{BaseInterfaceName}.cs` |
| Factory method | `static` method, **not** an interface impl, whose return type is the declaring type **or** whose return type is `bool` with an `out` parameter of the declaring type | `{Type}.Factory.cs` |
| Operator | Declared with `operator` keyword | `{Type}.Operators.cs` |
| Constructor | Named `{TypeName}(...)` | Root — stays |
| Everything else | Fields, properties, private helpers, nested types, etc. not covered above | Root — stays |

### Interface grouping rule
Interfaces that share a base name (ignoring generic arity and containing namespace) are grouped into one partial named after the shared base name. Examples:
- `IComparable<T>` + `IComparable` → `{Type}.IComparable.cs`
- `IEquatable<T>` + `IEquatable` → `{Type}.IEquatable.cs`

The partial's class declaration lists **all** grouped interfaces:
```csharp
public sealed partial class Duid : IComparable<Duid>, IComparable
```

## Partial file template (source)

```csharp
using System;

namespace My.Namespace
{
    /// <content>
    ///     <see cref="MyType"/> implementation of <see cref="IMyInterface{MyType}"/> and <see cref="IMyInterface"/>
    /// </content>
    public sealed partial class MyType : IMyInterface<MyType>, IMyInterface
    {
        // moved members
    }
}
```

Rules:
- Use `/// <content>` (not `/// <summary>`) on every non-root partial
- Content text: `<see cref="{Type}"/> implementation of <see cref="{Interface}"/>` — list all interfaces in the group
- For Factory: `<see cref="{Type}"/> static factory methods`
- For Operators: `<see cref="{Type}"/> operators`
- Repeat `using` directives needed by the members in this file
- Keep the same `namespace` as the root

## Test partial template

```csharp
using Xunit;

namespace My.Namespace.Tests
{
    /// <content>
    ///     <see cref="MyType"/> tests for <see cref="IMyInterface{MyType}"/> and <see cref="IMyInterface"/>
    /// </content>
    public partial class MyTypeTests
    {
        // moved or skeleton test methods
    }
}
```

- Test partials are `partial class {TypeName}Tests` (no interface list on the declaration)
- `/// <content>` describes which interface/grouping is under test
- Ambiguous tests that cannot be classified remain in the root test file with:
  ```csharp
  // TODO: manually move to appropriate test partial
  ```

## Test method classification (two-pass heuristic)

**Pass 1 — method name:** If the test method name contains the name of an interface method (e.g., `CompareTo`, `Equals`, `GetHashCode`, `ToString`, `Parse`, `TryParse`), assign it to the corresponding interface partial.

**Pass 2 — body scan:** If pass 1 is inconclusive, scan the test body for direct calls to interface methods. Assign to the first unambiguous match.

**Fallback:** If both passes are ambiguous or produce multiple matches, leave the test in the root file with a `// TODO` comment.

## csproj nesting — source project

```xml
<ItemGroup>
  <!-- Nest {Type}.*.cs under {Type}.cs -->
  <Compile Update="{Type}.IMyInterface.cs">
    <DependentUpon>{Type}.cs</DependentUpon>
  </Compile>
  <Compile Update="{Type}.Factory.cs">
    <DependentUpon>{Type}.cs</DependentUpon>
  </Compile>
  <Compile Update="{Type}.Operators.cs">
    <DependentUpon>{Type}.cs</DependentUpon>
  </Compile>
</ItemGroup>
```

## csproj nesting — test project

```xml
<ItemGroup>
  <!-- Nest {Type}.*Tests.cs under {TypeName}Tests.cs -->
  <Compile Update="{Type}.IMyInterfaceTests.cs">
    <DependentUpon>{TypeName}Tests.cs</DependentUpon>
  </Compile>
  <Compile Update="{Type}.FactoryTests.cs">
    <DependentUpon>{TypeName}Tests.cs</DependentUpon>
  </Compile>
  <Compile Update="{Type}.OperatorsTests.cs">
    <DependentUpon>{TypeName}Tests.cs</DependentUpon>
  </Compile>
</ItemGroup>
```

## Update mode rules

| Mode | Condition | Action |
|---|---|---|
| create missing | Partial for interface X does not exist on disk | Create the partial, move matching members |
| repair csproj | Partial exists on disk but `DependentUpon` entry is absent | Add the entry; do not modify source files |
| (skipped) | Member is in the wrong existing partial | Do nothing — never re-sort between existing partials |

## Validation

Abort with a clear error message when:
- The type implements/extends only one type (or none) — splitting is not meaningful
- The root file cannot be determined from context and the user does not provide it
- The root file is itself already a `{Root}.{Something}.cs` partial (ask user for the actual root)
