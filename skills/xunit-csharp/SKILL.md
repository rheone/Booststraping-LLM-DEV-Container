---
name: xunit-csharp
description: Write, update, and improve xUnit v3 unit tests in C# projects. Use when writing or reviewing C# unit tests, adding test coverage, or following project test standards.
author: Robert Engelhardt <rheone@gmail.com>
version: 1.0.0
---

# xUnit C# Testing Skill

## Scope Selection

Before writing or modifying tests, identify the focus level and test type:

| Scope           | Apply to                           |
| --------------- | ---------------------------------- |
| **Single test** | One `[Fact]` or `[Theory]` method  |
| **Group**       | Related tests within one `#region` |
| **Class**       | Entire test class file             |
| **Namespace**   | All test classes in a namespace    |
| **Project**     | All test classes in a project      |    
| **All tests**   | Every test file in the project     |

Confirm scope with the user if unclear.

**Test type:** This skill defaults to **unit tests**. If invoked standalone, ask at the start of the session:

> "Are these unit tests, or do some tests require real infrastructure (database, file system, network)? [Y = integration tests included / N = unit tests only]"

If invoked via `csharp-test-sweep`, this question was already answered in the sweep's upfront configuration — inherit that answer and skip this prompt.

If the answer is Y (either here or from the sweep), apply the additional integration test rules in the **Integration Tests** section below. All other rules remain in effect.

## Sub-Agent Parallelization

Use sub-agents to parallelize work when any of these triggers apply:

| Trigger | Threshold |
| ------- | --------- |
| Production class has many public members | ≥ 5 distinct public members |
| Test file is large or will grow large | Projected file length > 300 lines |
| Working at "all tests" scope | ≥ 3 test files in the project |

When a trigger applies, use this three-phase structure:

### Phase 1 — Discovery (parallel)
Spawn one `Explore` sub-agent per production file or namespace to:
- List all public/internal members
- Identify which members have a corresponding `#region` in the test file
- Report gaps (see **Coverage Gaps** section)

Collect all gap reports before proceeding.

### Phase 2 — Writing (parallel)
Spawn one sub-agent per test class to write or fill gaps. Each sub-agent receives:
- The production class it is responsible for
- The gap report from Phase 1
- The full rules from this skill

Do not spawn writing sub-agents until the user has confirmed the gap report (see **Coverage Gaps** section).

### Phase 3 — Verification (sequential)
After all writing sub-agents complete, run `dotnet test` (see **Verification** section). Fix failures before reporting complete.

## Core Rules

### File and Class Organization

- Prefer one test class per production class. `SubnetTests` tests only `Subnet`.
- Large test classes may be split into multiple files with a clear naming convention (e.g., `Subnet_ParseTests.cs`, `Subnet_TryParseTests.cs`) or broken into partial classes organized by member under test.
- Organize test methods into `#region` blocks named after the member under test:
    ```csharp
    #region Parse
    // all Parse tests
    #endregion
    ```
- Close each region with `#endregion` and ensure it contains only tests for that member.
- Test classes should not test members from other types.

### Naming

- Test methods: `{Member}_{Scenario}_{Expectation}_Test`
- MemberData fields: `{TestMethodName}_Test_Data`
- Examples: `Parse_ValidCidr_ReturnsSubnet_Test`, `Parse_NullInput_ThrowsArgumentNullException_Test`

### AAA Structure

Every test must follow Arrange / Act / Assert with comments:

```csharp
// Arrange
var input = "192.168.0.0/24";

// Act
var result = Subnet.Parse(input);

// Assert
Assert.Equal(IPAddress.Parse("192.168.0.0"), result.NetworkAddress);
```

### Documentation

Every test method must have an XML `<summary>` stating its intent. Every `TheoryData` field must have a `<summary>` documenting what each parameter represents.
Individual, or groups of theories, should document the rationale for the chosen test cases, especially if not exhaustive.

## Parameterized Tests

### `[Fact]` vs `[Theory]`

- Use `[Fact]` when the test has **exactly one meaningful scenario** and parameterizing it adds no information.
- Use `[Theory]` when **two or more distinct input/output combinations** exist — each mapping to a different equivalence partition or boundary value.
- **Never write a `[Theory]` with a single data row.** Convert it to `[Fact]`.

### Theory Data

- Prefer strongly-typed `TheoryData<T1, T2, ...>` with `[MemberData]` for all parameterized tests.
- `[InlineData]` is acceptable only for simple primitive literals. Use `[MemberData]` for everything else.
- Always use strongly-typed `TheoryData<T1, T2, ...>` — never `IEnumerable<object[]>`.
- `[ClassData]` is permitted but prefer `[MemberData]`; convert to `[MemberData]` on sweep when the change is low-risk.
- Test method parameters must be clearly named and match the order declared in `TheoryData<>`.

### Equivalence Partitioning

Choose theory data by **equivalence partitioning**: identify every distinct behavioral partition for each parameter and include exactly one representative case per partition, plus boundary values. Document which partition each row represents in the `<summary>`.

> Default is one case per partition. If the method under test has complex branching within a partition, add additional cases for that partition — state the reason in the `<summary>`.

```csharp
/// <summary>
/// Inputs: routePrefix (string), expectedLength (BigInteger).
/// Partitions: valid IPv4 CIDR, valid IPv6 CIDR, prefix at lower boundary (/0), prefix at upper boundary (/32 IPv4).
/// </summary>
public static TheoryData<string, BigInteger> Parse_ValidInput_ReturnsCorrectLength_Test_Data =>
    new()
    {
        { "192.168.0.0/24", 256 },         // valid IPv4 CIDR
        { "::/64",  new BigInteger(...) },  // valid IPv6 CIDR
        { "0.0.0.0/0", ... },              // lower boundary
        { "10.0.0.1/32", 1 },              // upper boundary IPv4
    };

/// <summary>Verifies Parse returns a subnet with the correct length for valid CIDR input.</summary>
[Theory]
[MemberData(nameof(Parse_ValidInput_ReturnsCorrectLength_Test_Data))]
public void Parse_ValidInput_ReturnsCorrectLength_Test(string routePrefix, BigInteger expectedLength) { ... }
```

See [references/theorydata.md](references/theorydata.md) for full conversion patterns (`IEnumerable<object[]>` → `TheoryData<>`), loop-generated data dedup, and parse-roundtrip correctness.

### Non-Serializable Types in Theories

If a type used in `TheoryData` or `MemberData` is not xUnit-serializable, implement `IXunitSerializer` and register it:

```csharp
[assembly: RegisterXunitSerializer(typeof(MyTypeXunitSerializer), typeof(MyType))]
```

See [references/IXunitSerializer.md](references/IXunitSerializer.md) for the full pattern.

## Exception Assertions

Use `Assert.Throws<T>` (sync) or `Assert.ThrowsAsync<T>` (async). Always verify the exception message or a relevant property to prevent false positives:

```csharp
var ex = Assert.Throws<ArgumentNullException>(() => Subnet.Parse(null));
Assert.Contains("input", ex.ParamName);
```

## Async Tests

- Async tests must return `Task`, never `async void`.
- If the method under test is async, the test must be async.
- `ConfigureAwait(false)` is not required in test methods.

### `CancellationToken` Testing

When a method under test accepts `CancellationToken`:

- Pass `CancellationToken.None` in tests where cancellation is **not** the subject. Never pass `default` — use `CancellationToken.None` explicitly for clarity.
- Write a dedicated test (or theory case) for cancellation: cancel a `CancellationTokenSource` before or during the call and assert `OperationCanceledException` is thrown.
- If the method checks `token.IsCancellationRequested` in a loop, test that it exits early when cancelled.

```csharp
[Fact]
public async Task ProcessAsync_CancelledToken_ThrowsOperationCanceledException_Test()
{
    // Arrange
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    // Act & Assert
    await Assert.ThrowsAsync<OperationCanceledException>(
        () => _sut.ProcessAsync(cts.Token));
}
```

## Assertions

### `Assert.Multiple`

Use `Assert.Multiple` when a **single logical scenario** has multiple independent properties to verify and you want the full failure picture in one run:

```csharp
// Assert
Assert.Multiple(
    () => Assert.Equal(expectedHead, result.Head),
    () => Assert.Equal(expectedTail, result.Tail),
    () => Assert.Equal(expectedLength, result.Length)
);
```

Never use `Assert.Multiple` to combine what should be separate test cases. If the scenarios have different inputs, they belong in a `[Theory]`.

### `Assert.Equivalent`

Use `Assert.Equivalent` when comparing complex objects where structural equality is needed but the type does not override `Equals`:

```csharp
Assert.Equivalent(expected, actual);
```

Use `Assert.Equal` when the type implements value equality (`Equals` / `IEquatable<T>`) or for primitives.

**Never override `Equals` on a production type solely to satisfy a test assertion.** Use `Assert.Equivalent` or `Assert.Multiple` with per-property assertions instead.

### Assertion Anti-Patterns

Every test method must contain at least one assertion. A `[Fact]` or `[Theory]` with no `Assert.*`, no `Received()`/`Verify()`, and no `Assert.Throws` passes vacuously and provides false confidence — flag and fix.

Replace imprecise boolean assertions with their specific equivalents:

| Anti-pattern | Replace with |
|---|---|
| `Assert.True(x == y)` | `Assert.Equal(y, x)` |
| `Assert.True(x != y)` | `Assert.NotEqual(y, x)` |
| `Assert.True(result != null)` | `Assert.NotNull(result)` |
| `Assert.True(result == null)` | `Assert.Null(result)` |
| `Assert.Equal(true, condition)` | `Assert.True(condition)` |
| `Assert.Equal(false, condition)` | `Assert.False(condition)` |
| `Assert.Equal(null, obj)` | `Assert.Null(obj)` |
| `Assert.NotEqual(null, obj)` | `Assert.NotNull(obj)` |

Specific assertions produce significantly better failure messages than boolean wrappers.

### Failure Messages

Add an assertion message **only when the default failure output would be ambiguous** — e.g. when the same assertion appears multiple times in one test, or when the asserted value doesn't make the failure cause obvious:

```csharp
Assert.True(result.IsValid, "IsValid should be true after successful parse");
Assert.True(result.HasValue, "HasValue should be set when parse succeeds");
```

Never add a message that merely restates what the assertion already says.

## Mocking

- Use the test project's existing mock library.
- **Never substitute the class under test** — substitutes are for dependencies. If the method under test is on an abstract base class, use a concrete subclass that inherits the implementation without overriding it.
- See the companion mocking skill for the detected library (e.g. [`nsubstitute-csharp`](../nsubstitute-csharp/SKILL.md), [`moq-csharp`](../moq-csharp/SKILL.md)), [`justmock-csharp`](../moq-csharp/SKILL.md)), or 
[`rhinomock-csharp/SKILL.md))
- Mock verification belongs in the Assert section.

## String Input Coverage

When a method accepts `string` parameters, include theory cases for:

- `null`
- `""` (empty)
- `" "` (whitespace)
- `"\t"`, `"\n"`, `"\r"` (control characters)
- Valid input (at least one representative value)

## Null Parameter Testing

For every public constructor or method parameter that is a reference type, test that passing `null` throws `ArgumentNullException` (or behaves as documented).

## Test Output

Use `ITestOutputHelper` only for **diagnostic context that helps interpret a failing test** — e.g. logging generated input in a fuzz-style test, or capturing timing data. Inject it via constructor:

```csharp
public class SubnetTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;
}
```

- Never use `ITestOutputHelper` as a substitute for assertions.
- Never use `Console.WriteLine` in tests — xUnit v3 suppresses it by default.
- Most tests do not need `ITestOutputHelper`. Do not inject it unless the diagnostic value is clear.

## Test Fixtures

Use the following decision table to choose the right setup mechanism:

| Situation | Use |
| --------- | --- |
| Setup is cheap and pure (no I/O, no shared state) | Constructor / `IDisposable.Dispose` |
| Setup or teardown is async (e.g. container startup, DB seed) | `IAsyncLifetime` — `InitializeAsync` / `DisposeAsync` |
| Setup is expensive but safe to share across tests **in one class** | `IClassFixture<T>` |
| Setup must be shared across **multiple test classes** (e.g. a shared DB container) | `ICollectionFixture<T>` |
| Each test must have a clean, isolated state regardless of cost | Constructor / `IDisposable.Dispose` always |

**Never call async methods synchronously** (`.GetAwaiter().GetResult()`, `.Result`) in test constructors — this can deadlock. Use `IAsyncLifetime` instead. See [xUnit documentation](https://xunit.net/docs/shared-context) for full wiring details.

Use `[Collection("name")]` to group test classes that must **not** run in parallel — e.g. classes that share a `ICollectionFixture<T>` or access the same external resource.

**All test classes sharing a `ICollectionFixture<T>` must use the same `[Collection("name")]` attribute.** Classes sharing a fixture type across different collection names (or no collection name at all) silently create multiple fixture instances instead of sharing one, causing flaky tests or resource exhaustion.

**Never** disable parallelism globally via `[assembly: CollectionBehavior(DisableTestParallelization = true)]`. It silences flakiness without fixing the isolation problem.

See [references/Fixtures.md](references/Fixtures.md) for full wiring examples.

## Object Mother

Use an Object Mother to provide **named, canonical instances** of a domain type when the same pre-built values are reused across multiple tests.

**Placement:**
- If the Object Mother is only used by **one test class**, colocate it with that class in the same folder.
- If it is likely to be used **across the test project**, place it under a `TestData/` subfolder. Name the file `{Type}Mother.cs` (e.g. `TestData/SubnetMother.cs`).

```csharp
internal static class SubnetMother
{
    public static Subnet TypicalIPv4()  => Subnet.Parse("192.168.0.0/24");
    public static Subnet FullIPv6()     => Subnet.Parse("::/0");
    public static Subnet SingleHost()   => Subnet.Parse("10.0.0.1/32");
}
```

Do not use an Object Mother for types that are trivially constructable inline. Do not use it as a substitute for theory data — if the test cares about the specific value, pass it explicitly.

See [references/ObjectMother.md](references/ObjectMother.md) for a full example.

## Coverage Gaps

When working at class or all-tests scope, run the following checklist **before writing any tests**. If using sub-agents, each sub-agent runs this checklist for its assigned class.

**Gap detection checklist:**

1. List every `public` and `internal` member of the production type.
2. For each member, confirm a matching `#region` exists in the test file.
3. For each `#region`, confirm it covers: at least one happy-path case, at least one failure/null/invalid path, and any documented boundary values.
4. For each branch (`if` / `switch` / ternary / `??`) in the member's body, confirm at least one test exercises that branch.
5. Compile the gap list: members missing a region, regions missing paths, branches missing coverage.

**Constructor coverage rule:** constructors tested implicitly through other members (e.g. a `Parse` test that returns the type) do not require a dedicated `#region .ctor`. Only add a `.ctor` region when the constructor has observable behavior worth testing directly — validation, argument guarding, or non-trivial initialization not covered by any other test.

**After compiling, present the gap list in plain text to the user and ask:**

> "The following coverage gaps were found: [list]. Should I implement the missing tests now? [Y/N]"

**If invoked via `csharp-test-sweep` with coverage mode set to `auto`**, skip this prompt and proceed to implement immediately. If coverage mode is `pause`, ask as above.

If N (or pause + user declines), record the gaps as a plain-text note and stop.

## Skipping Tests

`[Skip]` requires a written justification and an issue or ticket reference:

```csharp
[Fact(Skip = "Blocked by #42 — IXunitSerializer not yet implemented for Foo")]
```

## xUnit Version Upgrade (v1/v2 → v3)

If the project uses `xunit` or `xunit.core` (v1/v2), prompt the user to upgrade to `xunit.v3`. Key migration changes:

- `IClassFixture<T>` and `ICollectionFixture<T>` remain; constructor injection unchanged.
- `ITestOutputHelper` unchanged.
- Theory serialization: replace `IXunitSerializable` (v2) with `IXunitSerializer` + `[assembly: RegisterXunitSerializer]` (v3).
- Remove `[assembly: CollectionBehavior]` — xUnit v3 uses parallelism by default.
- `xunit.runner.visualstudio` → v3-compatible package.

## Integration Tests

> This section applies only if the user opted in during **Scope Selection**.

- Integration tests may use real infrastructure (database, file system, HTTP). Use `IClassFixture<T>` or `ICollectionFixture<T>` to manage expensive shared resources (see **Test Fixtures**).
- Each test must still leave infrastructure in a clean state. Use teardown in `IDisposable.Dispose` or `IAsyncLifetime.DisposeAsync`.
- Do not mock infrastructure in integration tests — the point is to verify the real interaction.
- Group all integration test classes that share a resource under one `[Collection]` to prevent parallel conflicts.
- Tag integration test classes with a trait to allow selective filtering:
    ```csharp
    [Trait("Category", "Integration")]
    ```

## Private/Internal Member Testing

1. Test through the public API first.
2. If insufficient, use `[assembly: InternalsVisibleTo("YourTests")]` for `internal` members.
3. Use reflection only as a last resort when neither of the above is feasible.

## Test Isolation

- No static mutable state shared between tests.
- Use the test class constructor for per-test setup; `IDisposable.Dispose` for teardown.
- Never use `Thread.Sleep` — arrange deterministic test conditions instead.

## Verification

After writing tests at any scope, run:

```shell
# Single class — pin to the highest modern TFM to avoid running all framework passes;
# tail -3 cuts through xUnit discovery output to the pass/fail line
dotnet test {project} --framework {highest-modern-tfm} --filter "FullyQualifiedName~{ClassName}" 2>&1 | tail -3
```

Or at project scope:

```shell
dotnet test {project}
```

- If tests fail, fix them before reporting the task complete.
- **Default behavior: fix failures automatically.** Surface them to the user only if the fix requires a design decision.
- Do not report success without a passing test run.


## Related Skills

This skill is invoked automatically by [`csharp-test-sweep`](../csharp-test-sweep/SKILL.md) when it detects xUnit in the project file. To run a full sweep that delegates here automatically