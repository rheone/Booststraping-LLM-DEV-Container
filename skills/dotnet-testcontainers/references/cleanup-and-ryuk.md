# Resource cleanup and Ryuk

## Two layers of cleanup

1. **Explicit disposal** — calling `DisposeAsync()` on a container (directly, or via the
   `IAsyncLifetime`/`await using` patterns in `references/lifecycle-and-xunit.md`) stops and removes
   it as part of the normal test run. This is the cleanup path that runs when tests complete
   successfully or throw an ordinary assertion failure.
2. **Ryuk, the resource reaper** — a small sidecar container Testcontainers starts automatically the
   first time any container is created in a test run. Every container, network, and volume
   Testcontainers creates is registered with Ryuk. If the test process crashes, is killed, or is
   debugged and abandoned mid-run — anything that skips explicit disposal — Ryuk detects the test
   process's session has ended and removes every resource it was tracking, so a crashed test run
   doesn't leave orphaned containers accumulating on the machine indefinitely.

Ryuk is why you don't need elaborate crash-recovery logic in test code: explicit disposal handles
the common case cleanly and immediately, and Ryuk is the backstop for the uncommon case where
disposal never gets a chance to run.

## Disabling Ryuk

Ryuk can be disabled via the `TESTCONTAINERS_RYUK_DISABLED` environment variable, needed in
environments where running a privileged sidecar container isn't permitted (some restricted CI
runners or container-in-container setups). Disabling it removes the crash-recovery backstop
entirely — every container then depends on explicit disposal for cleanup, and a crashed run leaves
orphaned containers that need manual `docker rm` cleanup. Only disable it where the environment
genuinely requires it, not as a default.

## Resource reaper sessions

Containers can be grouped under an explicit reaper session so a batch of related resources is
cleaned up together rather than tracked individually — relevant mainly for advanced setups running
many containers under shared lifecycle control. For ordinary test-class-scoped or fixture-scoped
containers (the shape in `references/lifecycle-and-xunit.md`), the default per-run session Ryuk
assigns automatically is sufficient and needs no manual configuration.

## What to check when containers seem to leak

If `docker ps -a` shows Testcontainers-created containers lingering after a test run:

- Confirm `DisposeAsync()` (or the `IAsyncLifetime`/`await using` pattern that calls it) actually
  runs on every code path, including a failed `InitializeAsync()` — a container that throws while
  starting inside `InitializeAsync` may never reach a paired `DisposeAsync` if the test framework's
  lifecycle doesn't call it on init failure; wrap risky startup logic in `try`/`finally` if a
  specific test framework's contract doesn't guarantee that call.
- Confirm Ryuk itself is running (`docker ps` should show a `testcontainers-ryuk-*` container during
  a test run) — if `TESTCONTAINERS_RYUK_DISABLED` is set somewhere in the environment, the crash
  backstop is gone and only explicit disposal cleans anything up.
