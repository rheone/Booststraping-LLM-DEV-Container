# Evals: dotnet-file-based-apps

Six eval cases and a trigger set for the [`dotnet-file-based-apps`](../../skills/dotnet-file-based-apps/README.md)
skill. They live outside the skill folder so they do not ship with the skill when users install it.

## Cases

| Case | Tests |
| --- | --- |
| `csproj-collision` | Explain why `dotnet run seed.cs` starts the project in the folder, and give the fix. |
| `fix-directive-error` | Fix a file whose `#:` directives use the wrong separators and a missing package version. |
| `outgrown-script` | Split a script into several files and keep `dotnet inventory.cs` working. |
| `script-with-package` | Write a CsvHelper script with no project, a pinned package, and directives first. |
| `shebang-linux` | Make `./cleanup.cs 14` run on Linux (shebang, LF, no BOM, `.gitattributes`, `chmod`). |
| `web-api-one-file` | Write a one-file minimal API with a source-generated JSON context (AOT-safe). |

`trigger-set.json` holds queries that should and should not trigger the skill.

## Layout

Each case folder holds:

- `prompt.md`: the user request.
- `case.yaml`: the expected outcome and the scaffold script.
- `graders/`: one file per check.
- `files/` and `place-files.sh`: the fixture and the script that copies it into the run directory. Cases without
  fixtures have neither.

## Known limits

- `place-files.sh` stays bash (`cp -R`). `claude plugin eval` runs `scaffold_script` through bash, and a `.cs`
  file-based app fails in its sandbox on Windows because the sandbox strips the environment. This is the one
  exception to the file-based-app rule.
- `claude plugin eval` requires `--eval-dir` inside the plugin root. These evals sit outside the skill folder, so
  you need a plugin root that holds both the skill and this folder. The repo root has no plugin manifest.

## Run

The case layout follows the `claude plugin eval` format (`case.yaml`, `prompt.md`, `graders/*.md`).
Point `--eval-dir` at this folder. `claude plugin eval --help` lists the options.
