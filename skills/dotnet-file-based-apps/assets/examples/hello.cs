// Minimal file-based app. Run: dotnet hello.cs -- Ada
var name = args.Length > 0 ? args[0] : "world";
Console.WriteLine($"Hello, {name}! (.NET {Environment.Version})");
