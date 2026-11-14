var days = args.Length > 0 ? int.Parse(args[0]) : 7;
var cutoff = DateTime.UtcNow.AddDays(-days);
foreach (var f in Directory.GetFiles("logs", "*.log"))
{
    if (File.GetLastWriteTimeUtc(f) < cutoff)
    {
        Console.WriteLine($"delete {f}");
        File.Delete(f);
    }
}
