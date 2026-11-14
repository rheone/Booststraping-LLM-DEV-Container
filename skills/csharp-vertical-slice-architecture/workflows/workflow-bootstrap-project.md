# Workflow: bootstrap a VSA project

Create a new .NET solution organized by slices, with one working slice, tests, and an architecture map.
Every step ends on a completion criterion; do not start the next step until it holds.

## 1. Read the environment

Check: installed SDKs (`dotnet --list-sdks`), an existing `global.json`, `Directory.Build.props`,
`.editorconfig`, and any solution or project already in the target folder. If a project exists, stop and
switch to [workflow-add-slice.md](workflow-add-slice.md) or [workflow-migrate.md](workflow-migrate.md).

**Done when** you know the SDK you will target, and the folder holds no project you would overwrite.

## 2. Ask the open decisions

Ask the user, one question at a time, only what step 1 and the request did not answer. Give the options
and the suggested default from the reference file for each:

| Decision | Options from | Suggested |
| --- | --- | --- |
| Target framework and language version | [platform-and-build.md](../references/platform-and-build.md) | `net11.0` / C# 15 if the SDK has it; `net10.0` for LTS |
| Edge types (HTTP, consumers, jobs, UI, gRPC, CLI) | [entry-points.md](../references/entry-points.md) | Minimal APIs |
| Layout | [project-layouts.md](../references/project-layouts.md) | A |
| Dispatch | [patterns.md](../references/patterns.md) | none; present all options |
| Data access and database | [data-access.md](../references/data-access.md) | none; present all options |
| Expected-outcome style | [results-and-errors.md](../references/results-and-errors.md) | none; present both |
| Validation | [validation.md](../references/validation.md) | built-in for Minimal APIs on .NET 10+ |
| Isolation level | [dependency-rules.md](../references/dependency-rules.md) | area-level sharing |
| Test framework and database container tooling | [testing.md](../references/testing.md) | the user's usual stack |
| Architecture tests (add or not) and tool | [architecture-tests.md](../references/architecture-tests.md) | offer; the user decides |

Scope is not an open decision. Build the use cases and inputs the request names and nothing more
(standing rule 7). Note extra inputs or use cases you would suggest for the final report.

**Done when** every row has an answer from the user or from the request. Record them; they fill
`ARCHITECTURE.md` in step 8.

## 3. Create the solution and projects

Use `dotnet new` templates (`web` or `webapi` for Minimal APIs, `webapiaot` when the user wants Native AOT,
`razor` for Layout C, `worker` for a job/consumer host). Create test projects for integration tests and,
if chosen, architecture tests. Add project references. Expose the host to tests with
`public partial class Program;` or `InternalsVisibleTo`. Add the async analyzer settings (CA1068, CA2016,
CA2007 for `src/`; CA2007 off for `tests/`) to `.editorconfig`, from
[slice-anatomy.md](../references/slice-anatomy.md#async-and-cancellation).

**Done when** `dotnet build` succeeds for the solution, and the analyzer settings are in `.editorconfig`.

## 4. Create the folder skeleton

Create the folders for the chosen layout: `Features/`, `Domain/`, `Infrastructure/Persistence/`,
`Infrastructure/Pipeline/` (or the layout's equivalents), and the mirrored `Features/` folder in each test
project. Remove template sample code (weather forecast) from `Program.cs`.

**Done when** the tree matches the chosen layout's tree in
[project-layouts.md](../references/project-layouts.md), and the build still succeeds.

## 5. Wire shared infrastructure

In `Program.cs` (or the host project), add only what the decisions call for:

- `AddProblemDetails()`, `AddExceptionHandler<T>()`, `UseExceptionHandler()`, `UseStatusCodePages()`.
- Validation (`AddValidation()` from the assembly that holds the request types, or the chosen library's
  filter or step).
- Data access registration (scoped context or session; unit of work if chosen).
- `TimeProvider.System` as a singleton.
- Authentication and authorization if the API is protected.
- The dispatcher and pipeline steps, if chosen.
- Telemetry source and meter, if the user asked for observability.
- Packages: add only those the decisions require, and list them for the user.

**Done when** the app starts (`dotnet run`), responds with 404 ProblemDetails for an unknown route, and the
build has no new warnings.

## 6. Add the first slice

Run [workflow-add-slice.md](workflow-add-slice.md) for one real use case from the user's request (or a
`GetHealthDetails`-style read slice if they gave none). Use
[slice.cs.tmpl](../assets/templates/slice.cs.tmpl) as the starting shape.

**Done when** that workflow's completion criterion holds.

## 7. Add architecture tests (if the user chose them)

Copy [ArchitectureTests.cs.tmpl](../assets/templates/ArchitectureTests.cs.tmpl) into the architecture-test
project, fill the placeholders, and adapt to the chosen tool and isolation level. Prove the rules can fail:
add a deliberate cross-slice reference, run the tests, see the failure, remove it.

**Done when** the architecture tests pass, and you observed each core rule fail once on a deliberate
violation.

## 8. Write the architecture map

Fill [ARCHITECTURE.md.tmpl](../assets/templates/ARCHITECTURE.md.tmpl) with the decisions from step 2 and
save it at the repo root as `ARCHITECTURE.md` (or append it to an existing `AGENTS.md` / `CLAUDE.md` /
`README.md` if the user prefers one file). Remove unused rows.

**Done when** no `{{PLACEHOLDER}}` remains, and every convention in the file matches the code you wrote.

## 9. Final check

Run `dotnet build` and `dotnet test` for the whole solution.

**Done when** both succeed with no new warnings, and you have told the user: the decisions taken, the
packages added, how to add the next slice, how to run the tests, and the inputs or use cases you would
suggest but did not build.
