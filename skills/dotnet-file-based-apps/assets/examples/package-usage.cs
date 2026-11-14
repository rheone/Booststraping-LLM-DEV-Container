#:package Humanizer@3.0.10

// Package reference with an exact version. Run: dotnet package-usage.cs
using Humanizer;

Console.WriteLine("file_based_app".Humanize());
Console.WriteLine(TimeSpan.FromMinutes(95).Humanize(precision: 2));
