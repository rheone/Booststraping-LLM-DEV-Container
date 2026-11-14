#!/usr/bin/env -S dotnet --

// Shebang script. Unix: chmod +x script.cs && ./script.cs --help
// Windows: dotnet script.cs -- --help
if (args.Length == 0 || args.Contains("--help"))
{
    Console.WriteLine("usage: script.cs <file>...   prints line counts");
    return args.Length == 0 ? 1 : 0;
}

var exitCode = 0;
foreach (var path in args)
{
    if (!File.Exists(path))
    {
        Console.Error.WriteLine($"not found: {path}");
        exitCode = 2;
        continue;
    }

    Console.WriteLine($"{File.ReadLines(path).Count(),8} {path}");
}

return exitCode;
