# TODO

Deferred CI/tooling ideas from the skill-catalog CI setup. None of these are wired up yet.
The active checks live in `.github/workflows/` (skill validation, markdown link check, CodeQL).

## CI additions worth considering

- **Mermaid diagram validation.** `mermaid-diagram-generator` ships its own
  `tools/validate-mermaid.mjs`. Running it in CI against every `.mmd` file and embedded
  ` ```mermaid ` block in the repo (starting with the naming-decoder flowchart in `SKILLS.md`)
  would catch a broken diagram before merge.
- **markdownlint + codespell for this repo's own PRs.** Both are already documented
  conventions in `AGENTS.md`, but only as part of the pre-commit config *deployed into a
  user's project* by `post-create.sh` — they don't run against this meta-repo's own content.
  Wiring the same two checks into a GitHub Actions workflow here would catch issues in PRs
  from anyone who didn't run pre-commit locally, which matters more now given the volume of
  prose across ~75 skill READMEs.
- **Commit `cspell.json` and run `cspell` in CI.** A `cspell.json` exists at the repo root but
  is untracked. Decide whether it's meant to be part of the repo; if so, commit it and add a
  CI step that runs `cspell` across the skill catalog's prose.
- **`ruff` for the handful of `.py` scripts** scattered across a few skills
  (`csharp-library-repo-structure`, `dotnet-nhibernate`, `legacy-dotnet-feature-mapper`). Small
  footprint, but cheap to add once there's a Python CI job anyway (the CodeQL workflow already
  sets one up).
- **PSScriptAnalyzer / ShellCheck** for the repo's ~5 `.ps1`/`.sh` files. Small footprint.
- **YAML validation** for `devcontainer.json` / `docker-compose.local.yml.example` and similar
  config files, matching the `check-yaml` pre-commit hook already documented for user projects.
- **A large-file check** (matching the 512KB threshold already documented for user projects),
  to catch an accidental large binary before it lands in history.

## Other open items

- Decide whether the `code_scanning` (CodeQL) requirement should eventually cover more than
  Python `build-mode: none` — the repo also has a handful of standalone `.cs` template files
  (not part of a real buildable project) that CodeQL currently doesn't scan.
