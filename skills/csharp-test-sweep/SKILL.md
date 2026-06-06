---
name: csharp-test-sweep
description: Systematically sweeps C# test classes one at a time, reviewing and improving them against quality standards. Detects the test framework and mocking library from the project file and delegates to companion skills. Handles multi-target-framework projects (net4x, netstandard, modern .NET) with conditional compilation awareness and .NET Framework environment gating. Supports targeted invocation by test, class, namespace, or full project. Use when asked to sweep, audit, review, or improve a test suite; when tests need a quality pass; or when adding coverage to an existing project.
author: Robert Engelhardt <rheone@gmail.com>
version: 1.0.1
---

# C# Test Sweep

## Scope

Scope is inferred from invocation arguments. Default is **full sweep**.

| Scope        | Invocation                                 | Behavior                                |
| ------------ | ------------------------------------------ | --------------------------------------- |
| Single test  | `/csharp-test-sweep ClassName.Method_Test` | One test method                         |
| Region/group | `/csharp-test-sweep ClassName#MemberName`  | One `#region` block                     |
| Class        | `/csharp-test-sweep ClassName`             | Full test class                         |
| Namespace    | `/csharp-test-sweep My.Namespace`          | All classes in that namespace/directory |
| Full sweep   | `/csharp-test-sweep`                       | All test projects                       |

- Narrower than class scope: skip sub-agent parallelization and the project-selection dialog
- Update mode applies at all scopes — improve existing tests, not just add missing ones

## Upfront Configuration

Ask these questions before starting. Skip those that don't apply to the scope or project.

**Always ask:**

1. _(Full / namespace scope only / class scope)_ "I found these test projects: [list]. Which should I include?"
2. "Auto-fill coverage gaps, or pause for approval per class? [auto / pause / no]"
    - If user chooses to fill gaps automatically warn that the tests will reflect the code as it currently exists, not necessarily the intended behavior; flag any gaps filled this way with `// Auto Generated, verify expected behavior:`
3. "Auto-fix quality violations, or flag only? [auto / flag]"
4. "Should I test `[Obsolete]`-marked members? [yes / no]"
5. "Do any tests in this project require real infrastructure (database, file system, network)? [yes / no]"

**Only when test project `<TargetFrameworks>` /<TargetFramework>`contains`net4x`**

6. "Is a working .NET Framework test runner available in this environment? [yes / no]"

**Only if baseline `dotnet build` fails:**

7. "The build has pre-existing failures. Fix them first, or proceed anyway? [fix / proceed]"

## Discovery Phase

Run once before the sweep loop. Skip for single-test and region scope.

1. **Find test projects** — locate `.csproj` files referencing `xunit.v3`, `xunit`, `NUnit`, or `MSTest.TestFramework`
2. **Detect frameworks** — read package refs per project; record the test framework (xUnit, NUnit, MSTest) and mocking library (NSubstitute, Moq, RhinoMocks, JustMock) for each project
3. **Detect build settings** — read `<TreatWarningsAsErrors>` per project
4. **Detect target frameworks** — read `<TargetFrameworks>`; note `net4x`/`netstandard` targets
5. **Group partial classes** — find `partial class` declarations; group all files belonging to the same logical class
6. **Audit theory serializers** _(xUnit only)_ — find all `TheoryData<T>` usages; for each non-primitive type check whether `[assembly: RegisterXunitSerializer(...)]` is registered; list any gaps
7. **Check InternalsVisibleTo** — for each production/test project pair, confirm `[assembly: InternalsVisibleTo]` covers the test project if `internal` members are under test; prefer public API
8. **Flag missing test files** — for each production class in scope, check whether a corresponding test file exists. List any production classes with no test file; present to user before sweep starts. Do not create files without user confirmation.
9. **Run baseline build** — `dotnet build {test-project}` for each selected test project; if it fails, ask question 7 above
10. **Detect duplicate test IDs** — run `dotnet test {project} --list-tests 2>&1 | sort | uniq -d` against each test project. Any output line is a duplicate display name that xUnit will silently skip. Present results as a table: `| File | Data source | Example duplicate display name |`. Treat each flagged data source as a required fix before the sweep touches that class.
11. **Audit skipped tests** — grep for `[Fact(Skip` and `[Theory(Skip` across all test files. Present as a table: `| Class | Test | Skip reason | Has ticket ref? |`. Flag any without a ticket reference as a required fix before the sweep touches that class.
12. **Audit assertion anti-patterns** — grep across all test files for: `Assert\.True\(.*==`, `Assert\.True\(.*!=`, `Assert\.Equal\(true,`, `Assert\.Equal\(false,`, `Assert\.Equal\(null,`, `Assert\.NotEqual\(null,`. Present a summary count by file. Not a blocker, but surfaces scope before the sweep starts.

Present all discovery findings before starting the sweep loop.

## Sweep Loop

Process classes in **directory order, then alphabetical within each directory**.

**Do not run `dotnet format`, `dotnet format analyzers`, or `csharpier` during the sweep.** The pre-commit hook handles formatting on staged files; running formatters mid-sweep produces noisy diffs across files the sweep hasn't touched.

**Skip non-test classes** — skip any file that contains no `[Fact]` or `[Theory]` methods. Known helper patterns to skip automatically: `*Mother`, `*Serializer`, `*Fixture`, `*Extensions`, `*Builder`, `*Helper`. List all skipped files in the Discovery findings so the user can override.

For each class:

1. **Dispatch to companion skills** — look up the companion skill for the detected test framework and mocking library. If no companion skill exists for a detected framework or library, inform the user: "No skill exists for [framework/library]. Continue with general quality rules only, or skip this project?" Apply all rules from each companion skill that is available.

    | Test framework | Companion skill        |
    | -------------- | ---------------------- |
    | xUnit          | `xunit-csharp`         |
    | NUnit          | `nunit-csharp`         |
    | MSTest         | `mstest-csharp`        |
    | Unknown        | No skill — inform user |

    | Mocking library | Companion skill        |
    | --------------- | ---------------------- |
    | NSubstitute     | `nsubstitute-csharp`   |
    | Moq             | `moq-csharp`           |
    | RhinoMocks      | `rhinomocks-csharp`    |
    | JustMock        | `justmock-csharp`      |
    | Unknown         | No skill — inform user |

2. **Apply general rules** — apply the [General Quality Checklist](references/quality-checklist.md) at every class regardless of framework. Check specifically: any test that mocks the class under test is broken — the mock intercepts the method under test and returns the type default instead of running real logic. See the detected mocking library's companion skill for the framework-specific pattern and fix.

3. **Sub-agents** — spawn a sub-agent per the thresholds defined in the detected test framework skill. Follow the [Sub-Agent Briefing Template](agents/subagents.md) exactly.

4. **Annotate ambiguities** — use `#warning {description}` if `TreatWarningsAsErrors` is off; use `// SWEEP-AMBIGUITY: {description}` otherwise. Both must include a comment explaining what the code does vs. what it should do.

5. **Obsolete members** — if user opted in: wrap every call to an `[Obsolete]` member with `#pragma warning disable CS0618` / `#pragma warning restore CS0618`. If not: keep existing tests as-is, add nothing new.

6. **Verify** — run `dotnet build {test-project}` then `dotnet test {project} --framework {highest-modern-tfm} --filter "FullyQualifiedName~{ClassName}" 2>&1 | tail -3`. Pin to the highest modern TFM (e.g. `net10.0`) to avoid running all four framework passes per class; `tail -3` cuts through test discovery noise to the pass/fail line. Auto-fix failures; surface to user only when unresolvable. **This step is mandatory after sub-agent output** — do not mark a class done without a passing build and test run. If `dotnet build` fails with errors throughout the file, restore the original with `git stash -- {file}` or `git checkout -- {file}` and make targeted improvements directly rather than editing a systematically broken file.

7. **Batch incompatible** — when no .NET Framework runner is available, collect the equivalent `dotnet test --framework net48` (or appropriate TFM) commands; emit them all at end of sweep.

## End of Sweep

1. Write a prose summary per class: what was reviewed, what changed, any ambiguities or gaps flagged
2. If .NET Framework runner was unavailable, output all batched `dotnet test --framework` commands in one block
3. Run full `dotnet test {test-project}` as a final verification pass

---

**Test framework skills:**

| Skill                                                  | Status                     |
| ------------------------------------------------------ | -------------------------- |
| [../xunit-csharp/SKILL.md](../xunit-csharp/SKILL.md)   | Full                       |
| [../nunit-csharp/SKILL.md](../nunit-csharp/SKILL.md)   | Stub — expand on first use |
| [../mstest-csharp/SKILL.md](../mstest-csharp/SKILL.md) | Stub — expand on first use |

**Mocking framework skills:**

| Skill                                                            | Status                     |
| ---------------------------------------------------------------- | -------------------------- |
| [../nsubstitute-csharp/SKILL.md](../nsubstitute-csharp/SKILL.md) | Full                       |
| [../moq-csharp/SKILL.md](../moq-csharp/SKILL.md)                 | Stub — expand on first use |
| [../rhinomocks-csharp/SKILL.md](../rhinomocks-csharp/SKILL.md)   | Stub — expand on first use |
| [../justmock-csharp/SKILL.md](../justmock-csharp/SKILL.md)       | Stub — expand on first use |

**Reference files:**

| Reference                                                          | Contents                                                                           |
| ------------------------------------------------------------------ | ---------------------------------------------------------------------------------- |
| [references/quality-checklist.md](references/quality-checklist.md) | General quality rules applied at every class                                       |
| [references/multiframework.md](references/multiframework.md)       | Conditional compilation, `#if` patterns, net48 batching, C# language version table |
| [agents/subagents.md](agents/subagents.md)                         | Sub-agent briefing template and sweep state tracking                               |
