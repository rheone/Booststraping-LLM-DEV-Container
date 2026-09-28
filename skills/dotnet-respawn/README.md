# Respawn

Respawn is a third-party library that resets a real database back to a clean state between
integration tests, without dropping and recreating the schema. This skill covers setting up a
checkpoint, keeping specific tables or schemas out of the reset, and wiring a reset into your test
suite's setup and teardown.

## When to reach for it

- Setting up a database reset for the first time so integration tests don't leak state into each
  other.
- Deciding which tables or schemas to exclude from a reset: reference or lookup data that should
  survive.
- Confirming Respawn supports the database engine a project targets.
- Wiring a checkpoint-and-reset cycle into a test fixture's setup and teardown.
- Resets are noticeably slow against a large schema and you need to scope or cache the checkpoint.

## Using it

This skill is model-invoked: it fires automatically when the situation matches, such as writing or
debugging integration test infrastructure that needs a clean database state. You can also invoke it
directly as `/dotnet-respawn`.

## What it covers

| Topic | Reference |
| --- | --- |
| Respawner.CreateAsync, RespawnerOptions, ResetAsync | [references/core-concepts.md](references/core-concepts.md) |
| Excluding tables and schemas from a reset | [references/table-schema-exclusion.md](references/table-schema-exclusion.md) |
| Supported database engines via DbAdapter | [references/supported-providers.md](references/supported-providers.md) |
| Wiring a reset into a test suite's setup/teardown | [references/fixture-integration-pattern.md](references/fixture-integration-pattern.md) |
| Reseeding cost, table-set scoping, connection reuse | [references/performance-considerations.md](references/performance-considerations.md) |
| Verifying a reset configuration clears and preserves the right tables | [references/testing.md](references/testing.md) |

## Example prompts

- "Set up a Respawn checkpoint for this SQL Server integration test suite."
- "Exclude the lookup and reference tables from the reset so seeded data survives between tests."
- "These resets are taking too long against our schema: how do I scope or cache the checkpoint?"
