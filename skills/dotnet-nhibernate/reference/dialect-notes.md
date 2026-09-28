# Dialect Notes

Everything else in this skill is written dialect-agnostically. If this team targets a single database engine consistently, state that explicitly wherever this skill is installed (a one-line note at the top of `SKILL.md`'s frontmatter description, or in the project's own README) — it removes a class of subtly-wrong advice this skill would otherwise give. If genuinely multi-dialect (e.g. SQL Server in production, a different engine in some test/dev contexts), read this file before assuming any of the following behave identically across environments.

## Where dialect differences bite most

| Area | Watch for |
|---|---|
| Identifier generation | `Identity` behaves differently across dialects in how it interacts with batching (see `mapping-conventions.md`) — some dialects support batched inserts with identity columns better than others. `HiLo` is dialect-independent, which is part of why it's this team's default. |
| String types | `NVARCHAR` vs `VARCHAR` vs `TEXT` vs dialect-specific unlimited-length types — a `.Length(n)` in a Fluent mapping doesn't guarantee the same storage/comparison behavior (case sensitivity, unicode handling) across dialects. |
| Paging (`Skip`/`Take`) | NHibernate's LINQ provider and QueryOver translate paging to the dialect's native syntax (`OFFSET/FETCH`, `LIMIT`, `ROWNUM`, etc.) — this is usually transparent, but if a query drops to native SQL for a hand-tuned case (see `query-strategy.md`), the paging syntax is no longer portable and that native SQL block needs a comment noting which dialect it assumes. |
| Case sensitivity | Some dialects are case-sensitive on string comparison/collation by default, some aren't — a `.Where(x => x.Name == "Foo")` that works in dev against one dialect's default collation may not match the same rows in a case-sensitive production configuration. Don't assume string equality is case-insensitive without checking the actual collation. |
| Boolean columns | Some dialects have a native `BIT`/`BOOLEAN` type, others represent it as an integer or char — legacy schemas (see `schema-mapping-roundtrip.md` § Direction 2) are the most common place this surfaces unexpectedly. |
| Date/time precision | Fractional-second precision on datetime columns varies by dialect and can cause round-trip equality checks (`entity.CreatedAtUtc == someStoredValue`) to fail if the DB truncates precision the in-memory `DateTime` doesn't. |

## If this skill is used across multiple projects on different dialects

Consider a per-dialect addendum file (`reference/dialect-sqlserver.md`, `reference/dialect-postgres.md`, etc.) rather than trying to caveat every reference file inline — same domain-organization pattern as any other progressive-disclosure split: load the one that matches the project you're actually in, not all of them. This skill ships without those addenda since the original team's target dialect wasn't specified when this was built — add one for your actual production dialect(s) as a natural first extension.
