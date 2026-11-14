# Architecture tests

Architecture tests turn the dependency rules into failing tests. Offer them in the bootstrap and migration
workflows; the user decides whether to add them. If the repo already has an architecture-test library or
an analyzer, use it (standing rule 2).

## Rule catalog

Core rules (offer these by default):

| # | Rule | Isolation level |
| --- | --- | --- |
| 1 | A slice does not depend on another slice | all |
| 2 | An area does not depend on another area | area-level, strict |
| 3 | Area `Shared/` does not depend on any slice | area-level |
| 4 | Domain does not depend on Features, Infrastructure, or ASP.NET Core | all |
| 5 | Shared infrastructure does not depend on Features | all |
| 6 | Legacy namespaces (`Services`, `Repositories`, `Controllers`) do not depend on Features | during migration |

Optional rules (offer when the repo's conventions call for them):

| # | Rule |
| --- | --- |
| 7 | Request, response, and outcome types are records |
| 8 | Handlers are `sealed` and `internal` |
| 9 | Edges (endpoints, controllers, consumers) do not depend on the `DbContext` / `ISession` directly |
| 10 | No public member in Features returns or accepts an ORM entity type |
| 11 | Types named `*Service` or `*Repository` do not live in Features (strict mode) |
| 12 | Every slice folder contains an edge (endpoint, page, consumer, job, command) |
| 13 | During migration: slices depend on legacy services only through an allow-list that shrinks over time |

## Choose the tool

| Tool | Kind | Status (2026-11-13) | Notes |
| --- | --- | --- | --- |
| ArchUnitNET (`TngTech.ArchUnitNET` + `.xUnit` / `.NUnit` / `.MSTestV2`) | Test library | 0.13.5, active | `ResideInNamespace` is exact; use `ResideInNamespaceOrBelow` for subtrees |
| NetArchTest.Rules | Test library | 1.3.2, no release since 2021 | Namespace matching is a case-insensitive prefix match |
| NetArchTest.eNhancedEdition | Test library (fork) | 1.4.5 | Adds `Slice().ByNamespacePrefix(...).Should().NotHaveDependenciesBetweenSlices()` |
| NsDepCop | Roslyn analyzer | — | Fails the build on illegal namespace dependencies; no test project |
| Assembly boundaries + `internal` | Compiler | — | Layouts D, E, F only |

## No library available

When the repo has no architecture-test library and the user has not approved adding one (standing rule
2), you can still write rules with plain reflection over the app assembly. Know the limit: reflection
sees type signatures (base types, interfaces, fields, properties, method parameters and return types,
attributes), not method bodies. A handler that does `new OtherSlice.Handler()` or calls a static method
in another slice inside a method body passes a reflection-only rule. State this limit in your report and
offer an IL-reading library (ArchUnitNET, NetArchTest) or a build-time analyzer (NsDepCop) as the
follow-up.

## Discover slices instead of listing them

Hard-coded slice lists go stale. Discover areas and slices from namespaces:

```csharp
const string FeaturesRoot = "MyApp.Features";

static string[] Areas(System.Reflection.Assembly assembly) =>
    [.. NamespacesUnder(assembly, FeaturesRoot)
        .Select(p => p.Split('.')[0])
        .Distinct()
        .Select(a => $"{FeaturesRoot}.{a}")];

static string[] Slices(System.Reflection.Assembly assembly) =>
    [.. NamespacesUnder(assembly, FeaturesRoot)
        .Select(p => p.Split('.'))
        .Where(p => p.Length >= 2 && p[1] != "Shared")
        .Select(p => $"{FeaturesRoot}.{p[0]}.{p[1]}")
        .Distinct()];

static IEnumerable<string> NamespacesUnder(System.Reflection.Assembly assembly, string root) =>
    assembly.GetTypes()
        .Select(t => t.Namespace)
        .OfType<string>()
        .Where(ns => ns.StartsWith(root + ".", StringComparison.Ordinal))
        .Select(ns => ns[(root.Length + 1)..]);
```

For Layout B (one file per slice, nested types), the slice is the static class, not a namespace. Discover
slices as the public or internal static classes directly in each area namespace, and compare declaring
types instead of namespaces.

## ArchUnitNET

Verified against `TngTech.ArchUnitNET` 0.13.5 on .NET 11.

```csharp
private static readonly Architecture Architecture =
    new ArchLoader().LoadAssemblies(typeof(Program).Assembly).Build();

[Fact]
public void Slices_do_not_depend_on_each_other()
{
    string[] slices = Slices(typeof(Program).Assembly);
    foreach (string from in slices)
    {
        foreach (string to in slices.Where(s => s != from))
        {
            Types().That().ResideInNamespaceOrBelow(from)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceOrBelow(to))
                .Because($"slice {from} must not use slice {to}")
                .Check(Architecture);
        }
    }
}

[Fact]
public void Domain_does_not_depend_on_features_or_infrastructure() =>
    Types().That().ResideInNamespaceOrBelow("MyApp.Domain")
        .Should().NotDependOnAny(Types().That().ResideInNamespaceOrBelow("MyApp.Features"))
        .AndShould().NotDependOnAny(Types().That().ResideInNamespaceOrBelow("MyApp.Infrastructure"))
        .Check(Architecture);
```

The built-in slice rule `SliceRuleDefinition.Slices().Matching("MyApp.Features.(*)..").Should()
.NotDependOnEachOther()` treats every distinct namespace under `Features` as its own slice, including each
area's `Shared` namespace. It enforces the **strict** level. For area-level sharing, use the pairwise rules
above. `Check` comes from the test-framework package (`TngTech.ArchUnitNET.xUnit` and others); the core
package offers `rule.HasNoViolations(architecture)` and `rule.Evaluate(architecture)`.

## NetArchTest.Rules

Two traps, both verified:

- `ResideInNamespace("MyApp.Features.Orders.GetOrder")` also matches `GetOrderById`, because matching is a
  prefix match. Filter with an anchored regex: `ResideInNamespaceMatching(@"^MyApp\.Features\.Orders\.GetOrder(\.|$)")`.
- A trailing dot in `HaveDependencyOnAny("MyApp.Features.Billing.")` matches nothing, so the rule passes
  when it should fail. Pass the namespace without a trailing dot.

```csharp
[Fact]
public void Slices_do_not_depend_on_each_other()
{
    System.Reflection.Assembly assembly = typeof(Program).Assembly;
    string[] slices = Slices(assembly);
    foreach (string from in slices)
    {
        string[] others = [.. slices.Where(s => s != from)];
        TestResult result = Types.InAssembly(assembly)
            .That().ResideInNamespaceMatching($"^{Regex.Escape(from)}(\\.|$)")
            .ShouldNot().HaveDependencyOnAny(others)
            .GetResult();

        Assert.True(result.IsSuccessful,
            $"{from} depends on another slice: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
```

The dependency side remains a prefix match: a dependency on `GetOrderById` also counts as a dependency on
`GetOrder`. In a rule where every other slice is forbidden, that only changes the message, not the result.

## NsDepCop (build-time)

```xml
<NsDepCopConfig>
  <Allowed From="*" To="*" />
  <Disallowed From="MyApp.Domain.*" To="MyApp.Features.*" />
  <Disallowed From="MyApp.Domain.*" To="MyApp.Infrastructure.*" />
  <Disallowed From="MyApp.Infrastructure.*" To="MyApp.Features.*" />
  <Disallowed From="MyApp.Features.Orders.*" To="MyApp.Features.Billing.*" />
  <Disallowed From="MyApp.Features.Billing.*" To="MyApp.Features.Orders.*" />
  <AnalyzerConfig><IssueKind Name="IllegalDependency" Severity="Error" /></AnalyzerConfig>
</NsDepCopConfig>
```

The ardalis `min-clean` template uses this approach (`config.nsdepcop`). Area-to-area rules must be listed
per pair; slice-to-slice rules inside an area are impractical to list by hand, so pair NsDepCop with a test
for rule 1.

## Migration allow-list

During a migration where slices may call legacy services temporarily, keep the exceptions in one place and
fail the test when the list grows:

```csharp
private static readonly string[] AllowedLegacyDependencies =
[
    "MyApp.Features.Orders.CreateOrder -> MyApp.Services.PricingService",   // remove when Pricing moves
];
```

Assert that the actual set of slice-to-legacy dependencies is a subset of the list, and that the list is
not longer than the previous commit's count (or record the count in the test).

## Verify the tests themselves

An architecture test that never fails protects nothing. When you add one:

1. Add a deliberate violation (a slice referencing another slice's type) in a scratch commit.
2. Run the tests and confirm the rule fails with a message naming both slices.
3. Remove the violation and confirm the tests pass.

The full test class is in [../assets/templates/ArchitectureTests.cs.tmpl](../assets/templates/ArchitectureTests.cs.tmpl).
