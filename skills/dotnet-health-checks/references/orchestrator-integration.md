# Container Orchestrator Integration

## What "liveness" means to an orchestrator

A liveness probe answers exactly one question: **is this process still functioning, or is it stuck/
crashed in a way only a restart fixes?** An orchestrator polls the liveness endpoint on an interval,
and if it fails enough consecutive times, the orchestrator's remedial action is to **kill and restart
the container**. This makes liveness the wrong place to report anything an orchestrator can't actually
fix by restarting — a downstream database being temporarily unreachable doesn't get better because
the app container restarts, so a liveness check that includes dependency status can trigger a restart
loop that makes an already-degraded situation worse without addressing the actual cause.

A liveness endpoint should therefore check only things a restart would genuinely remedy: an internal
deadlock, corrupted in-process state, a hung request pipeline. Many apps legitimately run a liveness
check that verifies nothing beyond "the process can respond to this HTTP request at all" (see the
`Predicate = _ => false` pattern in [tags-and-filtering.md](tags-and-filtering.md)).

## What "readiness" means to an orchestrator

A readiness probe answers a different question: **is this specific instance currently able to handle
real traffic?** An orchestrator polls the readiness endpoint and, on failure, **removes that instance
from the traffic-serving pool** without killing or restarting it — traffic simply stops being routed
to that instance until it reports ready again. This makes readiness the right place for dependency
checks (a database, a cache, a message broker) and for one-time startup gating: none of these
failures mean the process itself is broken, only that this instance shouldn't receive traffic right
now.

## Why the distinction changes the outcome

The practical difference is the orchestrator's response to a failure:

| Probe | On failure, the orchestrator... | Appropriate checks |
| --- | --- | --- |
| Liveness | Kills and restarts the container | Internal deadlock/hang detection; minimal or no dependency checks |
| Readiness | Stops routing traffic to this instance, without restarting it | External dependency reachability, one-time startup completion |

Registering a database check under a liveness endpoint (instead of readiness) is the single most
common way this goes wrong in practice: a transient database outage then causes every app instance to
be restarted repeatedly by the orchestrator, which does nothing to restore the database and adds
container-restart churn on top of an already-degraded dependency.

## Startup vs. steady-state readiness

Some orchestrators additionally distinguish a separate startup probe (checked only until the app first
reports ready, then never polled again) from an ongoing readiness probe (polled continuously
thereafter) — useful for an app whose startup sequence legitimately takes far longer than its
steady-state readiness check should ever take, without inflating the timeout tolerance used for
steady-state health. Whether a given orchestrator supports this as a distinct probe type or expects
the distinction folded into a single readiness endpoint is an orchestrator-specific configuration
detail outside this skill's scope — the health check side of it is the same either way: expose the
relevant checks (including the startup-gating check from
[tags-and-filtering.md](tags-and-filtering.md)) at whichever endpoint(s) the orchestrator is
configured to poll.
