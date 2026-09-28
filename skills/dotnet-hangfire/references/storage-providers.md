# Storage Providers

Hangfire has no in-memory job queue of its own by default — every enqueued job, its state, and its
schedule persist to a storage backend you configure, which is what makes jobs durable across an
application restart and shareable across multiple worker processes.

## The storage-provider concept

```csharp
builder.Services.AddHangfire(config => config
    .UseSomeStorageProvider(connectionStringOrOptions));

builder.Services.AddHangfireServer();
```

`AddHangfire` configures storage and job serialization; `AddHangfireServer` starts the worker
process that actually dequeues and executes jobs within this application instance. A relational
database, a distributed cache/key-value store, or another durable store can each serve as the
storage backend — Hangfire's job model (queues, schedules, state history) is the same regardless of
which one backs it; only the connection configuration and the store's own operational
characteristics (throughput, polling vs. push-based dequeue, retention/cleanup behavior) differ
between backends.

## Choosing a backend, generically

Weigh a storage backend against how your application already scales and what it already operates:

- A backend you already run in production adds no new operational surface, versus introducing a
  storage technology solely for job persistence.
- A relational store gives strong consistency and easy ad hoc querying of job history at the cost
  of write throughput under very high job volume.
- A distributed cache/key-value store gives higher throughput for high-volume job workloads at the
  cost of a different consistency and durability model than a relational database.

This skill does not recommend one backend over another — that decision depends on what your
application's infrastructure already looks like, not on anything specific to Hangfire's own API.

## Multiple servers share one storage instance

Every `AddHangfireServer()` instance — whether in the same process or a separate deployed instance
— that points at the same storage configuration participates in dequeuing and processing the same
job queues. This is how Hangfire scales horizontally: point multiple application instances at the
same storage backend and each instance's server competes for and processes jobs, with the storage
layer responsible for ensuring one job is claimed by exactly one worker at a time.

## Job data lives in storage, not in memory

Because job state (queued, scheduled, processing, succeeded, failed, deleted) lives entirely in the
configured storage, restarting the application process does not lose queued or scheduled jobs —
they remain in storage and resume being processed once a server starts back up and reconnects.
