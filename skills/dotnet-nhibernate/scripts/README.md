# Detector Scripts

All of these are **heuristic, static, regex-based first-passes** — none of them do real Roslyn
semantic analysis (session lifetime tracking, type resolution, etc.). That's a reasonable v1
tradeoff: they're cheap to run, cheap to maintain, and good enough to catch the shapes that
matter most on this team's codebase, but they will have false positives and false negatives.
Always present findings as "worth a look," never as a verdict.

## Status

| Script | Status | What it does |
|---|---|---|
| `detect_lazy_after_close.py` | Implemented (MVP) | Flags nested property access on a variable after its owning `using (session...)` block appears to have closed, in the same file |
| `detect_cascade_misconfig.py` | Implemented (MVP) | Pairs up `HasMany`/`References` across mapping files by `KeyColumn`/`Column` name, flags missing or doubled `.Inverse()` |
| `detect_n_plus_one.py` | Implemented (MVP) | Flags nested property access inside a `foreach` loop — doesn't yet cross-reference against mapping files to confirm the property is actually lazy |
| `detect_sync_over_async.py` | Implemented (MVP) | Flags `.Result`/`.Wait()`/`.GetAwaiter().GetResult()` on lines with NHibernate-ish tokens, and `async void` methods. See `reference/async-patterns.md`. |
| `detect_automapper_lazy_risk.py` | **Not yet implemented** — described in reference/lazy-loading-and-fetching.md as a planned check. Would parse AutoMapper `CreateMap<TSource, TDest>()` profiles and cross-reference `.ForMember` paths against lazy-mapped properties in the source entity's mapping file. Natural next build — could share the mapping-file parsing already written for `detect_cascade_misconfig.py`. |
| `detect_mapping_schema_drift.py` | **Not yet implemented** — described in reference/schema-mapping-roundtrip.md as a planned check. Would need a live connection string or an exported schema (e.g. `INFORMATION_SCHEMA` dump) to diff against mapped columns; the mapping-side parsing can reuse the same approach as the two scripts above. |

Treat the last two as the natural v2 additions for this skill — they're referenced from the
reference files as if generally available, which is intentional (that's where they *should* be
used from a workflow perspective), but be upfront with the user that they'd need to be built
before the "Full review workflow" section of SKILL.md can run them for real.

## Usage

```bash
python detect_lazy_after_close.py path/to/Services/
python detect_cascade_misconfig.py path/to/Mappings/
python detect_n_plus_one.py path/to/Services/
```

All three accept a single file or a directory (recursively scanned for `*.cs` / `*Map.cs`).
No dependencies beyond the Python standard library — deliberately, so they run anywhere without
a setup step getting in the way of "just run it during review."

## Extending these

If you build out the two not-yet-implemented scripts, or improve the false-positive rate on the
existing three, the shared pattern worth keeping: parse mapping files into a small structured
index (entity → columns → lazy/eager, keyed by table/column name) once, and have every detector
that needs mapping awareness consume that same index rather than re-parsing ad hoc per script.
`detect_cascade_misconfig.py`'s `key_column_index` is a reasonable starting point to factor out
into a shared module if a fourth or fifth detector gets added.
