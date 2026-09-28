# Elevated Mocking Setup

Elevated Mocking is JustMock's profiler-based mode that intercepts calls at the CLR level rather
than through interface/virtual-dispatch substitution — it's what makes mocking a `static` method, a
`sealed` class, or a non-virtual member on a concrete class possible at all. It requires the
commercial edition and an active CLR profiler for the test process.

## What Elevated Mocking unlocks

- **Static classes and static methods** — including utility classes with no interface at all.
- **Sealed classes** — normally un-mockable by proxy-based frameworks since there's nothing to
  subclass.
- **Non-virtual members** on an otherwise ordinary class — no need to make a member `virtual` just
  to make it testable.
- **Non-public members** (`private`/`internal`) directly, without exposing them via `InternalsVisibleTo`
  or reflection helpers.
- **Select BCL/mscorlib types** — `DateTime`, `File`, and similarly hard-to-abstract framework types
  — directly, without wrapping them in a custom interface first.

## Enabling the profiler

The CLR profiler must be active in the process **before** the test host starts — it cannot be
attached after the process is already running. This is done through environment variables set for
the test-runner process:

| Variable | Purpose |
| --- | --- |
| `JUSTMOCK_INSTANCE` | Must be set to a non-zero numeric value — this is the JustMock-specific signal that elevated mocking should activate for this process. |
| `CORECLR_ENABLE_PROFILING` | Set to `1` to enable CLR profiling for .NET (Core)/5+ test hosts. |
| `CORECLR_PROFILER` | The GUID identifying the JustMock profiler CLSID (`{B7ABE522-A68F-44F2-925B-81E7488E9EC0}`). |
| `CORECLR_PROFILER_PATH` | Full filesystem path to `Telerik.CodeWeaver.Profiler.dll` (the architecture-matching build — x86/x64/ARM64 — for the test host process). |

Set all four before the test process starts (a CI pipeline step, a `.runsettings`/launch profile
environment block, or the shell environment in a local terminal session) — setting them from inside
test code after the process has already started the CLR has no effect, since the profiler attaches
at process/runtime startup.

## Installation-free configuration

JustMock also supports pointing the environment variables directly at the profiler DLL from a
JustMock installation or a NuGet-restored package path, without running a separate installer on the
machine running the tests — this is the practical approach for CI agents, where installing a full
Telerik product isn't desirable. Point `CORECLR_PROFILER_PATH` at the `Telerik.CodeWeaver.Profiler.dll`
that ships alongside the commercial JustMock package reference in the restored NuGet package
directory.

## IDE and test-runner integration

Running elevated-mocking tests from Visual Studio's Test Explorer, `dotnet test`, or an IDE like
Rider each need the profiler environment variables present in *that specific process's*
environment — a variable set in one terminal session or one IDE's run configuration does not
automatically apply to a different launcher. The JustMock Visual Studio extension configures this
for Visual Studio's own test host automatically once installed and enabled per test project; other
runners need the four variables set explicitly in their own environment/configuration mechanism.

## Common failure: "the profiler must be enabled"

This runtime error means a test attempted an elevated-mocking arrangement (mocking a static/sealed/
non-virtual target) while the profiler was not active for the current process. Causes, in order of
likelihood:

1. One or more of the four environment variables above is missing or has an incorrect value for the
   current process launch (most common on CI, where the pipeline step setting them doesn't scope to
   the actual test-execution step).
2. A **third-party profiler is already active** in the same process (code-coverage tools and other
   profiling-based agents commonly register their own `CORECLR_PROFILER`) — only one CLR profiler
   can be active at a time, so JustMock's must either be the sole registered profiler or be chained
   through JustMock's own profiler-linking configuration if the other tool supports it.
3. The test host process was already running before the environment variables were set — restart
   the process (not just re-run the tests in an already-warm host) after correcting the environment.

## Common pitfall

Elevated Mocking configuration is per-process, not per-test or per-assembly — a test project mixing
ordinary interface/virtual mocks (which work identically with or without the profiler) and elevated
mocks (static/sealed/non-virtual targets) needs the profiler active for the *entire* test run if any
single test in that run needs it; there's no way to scope the profiler to just the tests that need
elevation within one process.
