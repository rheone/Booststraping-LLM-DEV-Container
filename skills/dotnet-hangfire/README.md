# Hangfire

Guidance on Hangfire, the background job library for .NET — the routing table (by task, not
Hangfire version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per Hangfire version

| File | Covers |
| --- | --- |
| `core-concepts.md` | `BackgroundJob.Enqueue`/`Schedule`/`ContinueJobWith`, `RecurringJob.AddOrUpdate` |
| `storage-providers.md` | The storage-provider concept behind job/state persistence |
| `dashboard.md` | `UseHangfireDashboard`, dashboard authorization |
| `job-filters-and-retry.md` | `IJobFilter`, `AutomaticRetryAttribute`, retry/backoff behavior |
| `testing.md` | Asserting a job was enqueued with the right method and arguments |

## Scope

Hangfire only — enqueuing, scheduling, and processing background jobs from a .NET process. Out of
scope: a specific storage backend's own operational tuning and running the full job pipeline inside
a unit test (see [SKILL.md](SKILL.md) for why).

Each reference file notes a version-introduced fact inline; version is not the file-splitting axis
for this skill. Hangfire's core license carries a non-standard commercial tier alongside its
open-source license — research current terms independently before adopting it for a commercial
project.
