# Evals: csharp-vertical-slice-architecture

Six eval cases for the [`csharp-vertical-slice-architecture`](../../skills/csharp-vertical-slice-architecture/README.md)
skill. They live outside the skill folder so they do not ship with the skill when users install it.

## Cases

| Case | Tests |
| --- | --- |
| `add-cancel-order-slice` | Add a slice to a .NET 11 repo that follows its own conventions. |
| `add-slice-net10-single-file` | Add a slice to a .NET 10 repo with one file per use case (no C# 15 unions). |
| `bootstrap-library-api` | Start a new API, and add only what the prompt names. |
| `dbcontext-vs-repository` | Answer a data access question with options and trade-offs. |
| `migrate-layered-orders` | Move a layered project to slices without changing observable behavior. |
| `review-smelly-slices` | Find the planted smells in an existing codebase. |

## Layout

Each case folder holds:

- `prompt.md`: the user request.
- `case.yaml`: the expected outcome and the scaffold script.
- `graders/`: one file per check.
- `files/` and `place-files.sh`: the fixture repo and the script that copies it into the run directory.

## Fixtures

The fixtures stand in for existing repos. They intentionally do not follow the skill's async rule
(`CancellationToken ct = default`, `ConfigureAwait(false)`). Do not copy them as examples.

## Run

The case layout follows the `claude plugin eval` format (`case.yaml`, `prompt.md`, `graders/*.md`).
Point `--eval-dir` at this folder. `claude plugin eval --help` lists the options.
