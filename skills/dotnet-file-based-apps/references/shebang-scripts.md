# Shebang scripts

On Linux and macOS a file-based app runs as an executable script: `./tool.cs`. On Windows the `#!` line is allowed and ignored; run the file with `dotnet tool.cs`.

## The recipe

```csharp
#!/usr/bin/env -S dotnet --
#:package Spectre.Console@0.57.2

using Spectre.Console;

AnsiConsole.MarkupLine($"[green]args:[/] {string.Join(' ', args)}");
return 0;
```

```bash
chmod +x tool.cs
./tool.cs --help other args      # the app receives ["--help", "other", "args"]
```

1. **Line 1 is exactly `#!/usr/bin/env -S dotnet --`.** The `--` stops `dotnet` from taking options like `--help` that belong to the app. Directives follow on the next lines.
2. **No BOM.** With a BOM before `#!`, the shell does not see a shebang. It runs the file as `sh` and fails with `#!/usr/bin/env: not found` (`dotnet tool.cs` still works, which hides the problem).
3. **LF line endings.** With CRLF, `#!/usr/bin/env dotnet` fails with `env: 'dotnet\r': No such file or directory`. Save the file as LF even when you edit on Windows.
4. **Executable bit.** Run `chmod +x`. To keep the bit in git, run `git update-index --chmod=+x tool.cs`.
5. **The file can drop `.cs`.** An extensionless file with the shebang (`./mytool`) also runs, and `dotnet run --file mytool` accepts it.

If an old `env` has no `-S`, use `#!/usr/bin/env dotnet`. It works, but `dotnet` then sees options such as `--help` before the app does.

## Keeping LF in a repository

Check the repo's `.gitattributes` before you commit a shebang script. A rule like `*.cs text eol=crlf` checks the file out with CRLF and breaks it on Unix. Add a narrower rule for the scripts folder:

```gitattributes
# scripts/.gitattributes
*.cs text eol=lf
```

Also check `.editorconfig`. `charset = utf-8-bom` or `end_of_line = crlf` for `*.cs` makes editors rewrite the file. Override both for the scripts folder:

```ini
[scripts/**.cs]
charset = utf-8
end_of_line = lf
```

To check a file: `head -c 3 tool.cs | od -c` shows `#   !   /` (no `357 273 277` BOM bytes). `file tool.cs` must not report "with CRLF line terminators".

## Shebang outside Unix scripts

The shebang also marks the entry file of a multi-file app (`#:include`, `#:ref`) for the IDE and for analyzer CA2266. Add it to those entry files on every OS. See [multi-file.md](multi-file.md).
