// Copies a case's files/ folder into the current directory (the eval run directory).
// Each case's place-files.sh calls this with its own folder as the only argument.
if (args.Length != 1)
{
    Console.Error.WriteLine("usage: dotnet place-files.cs -- <case-dir>");
    return 2;
}

var source = Path.Combine(Path.GetFullPath(args[0]), "files");
var target = Directory.GetCurrentDirectory();

foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
{
    Directory.CreateDirectory(Path.Combine(target, Path.GetRelativePath(source, dir)));
}

foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
{
    File.Copy(file, Path.Combine(target, Path.GetRelativePath(source, file)), overwrite: true);
}

return 0;
