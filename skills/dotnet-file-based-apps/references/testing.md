# Verifying a file-based app

A file-based app has no test project. You verify it by building it, running it with known input, and checking its output and exit code. When it needs real unit tests, that is a trigger to convert ([convert-to-project.md](convert-to-project.md)).

## The check, every time

Run these before you report that a file-based app works:

1. **SDK band.** `dotnet --version` from the file's folder (so `global.json` applies). Confirm every directive you used is supported on that band ([directives.md](directives.md)).
2. **Build, and read the warnings.** `dotnet build app.cs`. The build must have 0 errors. Read each warning:
   - IL2026 / IL3050 in your own code: fix it. The code fails at run time ([build-environment.md](build-environment.md)).
   - IL2xxx / IL3xxx from a package: add `#:property PublishAot=false` with a comment, or switch packages.
   - CA2266: add the shebang to the entry file.
   - NU1015 / NU1008: fix the `#:package` version form for the repo's CPM setting.
3. **Run with real arguments.** `dotnet app.cs -- <args>`. Check stdout and `echo $?` (PowerShell: `$LASTEXITCODE`). Include one failure case (missing file, bad input) and check that it exits non-zero.
4. **Run it the way the user runs it.**
   - Shebang scripts: on Unix, `chmod +x` then `./app.cs`. Check the BOM and line endings ([shebang-scripts.md](shebang-scripts.md)).
   - Next to a `.csproj`: run with `dotnet app.cs` or `--file`, and confirm the output is the script's, not the project's.
   - Web apps: start it, call each endpoint, check the status and body (below).
5. **Publish, only if the user publishes.** `dotnet publish app.cs` needs the native AOT toolchain. A publish failure about `clang`/`gcc` is a missing toolchain, not a code bug.

## Scenario: a CLI script

```bash
dotnet build tool.cs                      # 0 errors; read the warnings
dotnet tool.cs -- sample.csv; echo $?     # expected output, exit 0
dotnet tool.cs -- missing.csv; echo $?    # error on stderr, non-zero exit
```

On Unix, if it's a shebang script, also run `./tool.cs sample.csv`.

## Scenario: a one-file web API

```bash
dotnet build api.cs
dotnet api.cs --urls http://127.0.0.1:5080 &
sleep 5
curl -s -w ' [%{http_code}]\n' http://127.0.0.1:5080/
curl -s -w ' [%{http_code}]\n' http://127.0.0.1:5080/hello/Ada
kill %1
```

Call **every endpoint that returns an object**. A clean build doesn't prove JSON works: with AOT on, a type missing from the `JsonSerializerContext` returns 500 only at run time. On a 500, read the server log for the `JsonTypeInfo metadata … was not provided` message.

On Windows, start the server with `Start-Process dotnet -ArgumentList 'api.cs','--urls','http://127.0.0.1:5080'` and call it with `Invoke-WebRequest`.

## Scenario: a multi-file app

```bash
dotnet --version                     # 10.0.300+ for #:include; #:ref needs the flag on 10.0.3xx/4xx
dotnet build main.cs                 # no CA2266, no duplicate-directive errors
dotnet main.cs -- <args>
```

After you add or rename an included file, rebuild with `--no-cache`. A `#:include` glob turns caching off, so you only need this for explicit paths.

## Testing on a different SDK band

To check the file on a band other than the installed one, run it in the official SDK image:

```bash
docker run --rm -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet app.cs
docker run --rm -v "$PWD:/src" -w /src mcr.microsoft.com/dotnet/sdk:11.0 dotnet app.cs
```

Or pin a band with `global.json` in a scratch folder (`"rollForward": "disable"`).
