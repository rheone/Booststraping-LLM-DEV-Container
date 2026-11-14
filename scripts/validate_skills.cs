#!/usr/bin/env -S dotnet --
#:package YamlDotNet@18.1.0
#:property PublishAot=false

// Validate skill structure, frontmatter, and insularity across skills/.
//
// Two checks:
// 1. Structure/frontmatter: every skills/<dir>/SKILL.md has valid YAML frontmatter with the required
//    fields, and its `name:` matches the directory name exactly. Every skill directory also has a README.md.
// 2. Insularity: a skill's own files must not name a sibling skill's directory slug.
//    csharp-skill-authoring (the meta-skill, which documents the naming convention using real skill names
//    as worked examples) and csharp-test-sweep (the orchestrator, which names its own nested companions)
//    are excluded from the insularity check.
//
// Files that git ignores (local eval workspaces, build output) are skipped.
// Exits non-zero and prints every violation if any check fails.
//
// Run: dotnet scripts/validate_skills.cs   (.NET 10 SDK or later)

using System.Diagnostics;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

var repoRoot = FindRepoRoot(Directory.GetCurrentDirectory());
if (repoRoot is null)
{
    Console.Error.WriteLine("Run this from inside the repository (a folder with skills/ and scripts/ above it).");
    return 2;
}

string[] requiredFields = ["name", "description", "license"];
HashSet<string> insularityExempt = ["csharp-skill-authoring", "csharp-test-sweep"];

// Skills that predate this catalog's conventions (already merged to main before the naming/structure/
// testability standard existed) or that are not part of the C#/.NET skill catalog at all. Out of scope for
// this validator until someone deliberately brings them up to the current standard.
HashSet<string> notYetConforming =
[
    "audit-remediation-pipeline",
    "csharp-code-organization",
    "csharp-docs-and-comments",
    "csharp-library-repo-structure",
    "csharp-split-type-to-partials",
    "csharp-test-sweep",
    "csharp-union",
    "dispatch-tasks",
    "dotnet-nhibernate", // no README.md yet; predates the current template
    "github-markdown",
    "legacy-dotnet-feature-mapper",
    "mermaid-diagram-generator",
    "reverse-engineered-docs",
];

var files = ListFiles(repoRoot, "skills");
var skillDirs = files
    .Where(f => Path.GetFileName(f) == "SKILL.md")
    .Select(f => f[..^"/SKILL.md".Length])
    .Where(d => !notYetConforming.Contains(d.Split('/')[1]))
    .Order(StringComparer.Ordinal)
    .ToList();

if (skillDirs.Count == 0)
{
    Console.WriteLine("No skill directories found under skills/ - nothing to validate.");
    return 0;
}

var structureErrors = CheckStructure(skillDirs, files);
var insularityErrors = CheckInsularity(skillDirs, files);

if (structureErrors.Count + insularityErrors.Count == 0)
{
    Console.WriteLine($"OK: {skillDirs.Count} skills passed structure and insularity checks.");
    return 0;
}

Console.WriteLine($"FAIL: {structureErrors.Count + insularityErrors.Count} issue(s) across {skillDirs.Count} skills:\n");
foreach (var error in structureErrors)
{
    Console.WriteLine($"  [structure]  {error}");
}
foreach (var error in insularityErrors)
{
    Console.WriteLine($"  [insularity] {error}");
}
return 1;

List<string> CheckStructure(List<string> dirs, List<string> allFiles)
{
    var errors = new List<string>();
    var deserializer = new DeserializerBuilder().Build();
    foreach (var dir in dirs)
    {
        var skillMd = $"{dir}/SKILL.md";
        var text = File.ReadAllText(Path.Combine(repoRoot, skillMd));
        Dictionary<object, object>? frontmatter = null;
        var parts = text.StartsWith("---", StringComparison.Ordinal) ? text.Split("---", 3) : [];
        if (parts.Length == 3)
        {
            try
            {
                frontmatter = deserializer.Deserialize<Dictionary<object, object>?>(parts[1]) ?? [];
            }
            catch (YamlDotNet.Core.YamlException)
            {
                frontmatter = null;
            }
        }

        if (frontmatter is null)
        {
            errors.Add($"{skillMd}: missing or unparsable YAML frontmatter");
            continue;
        }

        foreach (var field in requiredFields.Where(f => !frontmatter.ContainsKey(f)))
        {
            errors.Add($"{skillMd}: frontmatter is missing required field '{field}'");
        }

        var dirName = dir.Split('/')[^1];
        if (frontmatter.TryGetValue("name", out var declared) && declared?.ToString() != dirName)
        {
            errors.Add($"{skillMd}: frontmatter name '{declared}' does not match directory name '{dirName}'");
        }

        if (!allFiles.Contains($"{dir}/README.md"))
        {
            errors.Add($"{dir}: missing README.md");
        }
    }
    return errors;
}

List<string> CheckInsularity(List<string> dirs, List<string> allFiles)
{
    var allSlugs = dirs.Select(d => d.Split('/')[^1]).ToHashSet();
    var errors = new List<string>();
    foreach (var dir in dirs)
    {
        var parts = dir.Split('/');
        // A companion nested under an exempt orchestrator's own skills/ directory is part of its exempt structure.
        if (parts.Any(insularityExempt.Contains))
        {
            continue;
        }

        var others = allSlugs.Where(s => s != parts[^1]).ToList();
        if (others.Count == 0)
        {
            continue;
        }

        var pattern = new Regex(@"(?<![\w-])(" + string.Join("|", others.Select(Regex.Escape)) + @")(?![\w-])");
        foreach (var md in allFiles.Where(f => f.StartsWith(dir + "/", StringComparison.Ordinal) && f.EndsWith(".md", StringComparison.Ordinal)))
        {
            var text = File.ReadAllText(Path.Combine(repoRoot, md));
            foreach (Match match in pattern.Matches(text))
            {
                var line = text.AsSpan(0, match.Index).Count('\n') + 1;
                errors.Add($"{md}:{line}: names sibling skill '{match.Groups[1].Value}' - skills must be insular (see csharp-skill-authoring/references/insularity.md)");
            }
        }
    }
    return errors;
}

// Tracked and untracked-but-not-ignored files under a folder, as repo-relative '/' paths.
static List<string> ListFiles(string root, string folder)
{
    var psi = new ProcessStartInfo("git", ["-c", "core.quotepath=off", "ls-files", "--cached", "--others", "--exclude-standard", "-z", "--", folder])
    {
        WorkingDirectory = root,
        RedirectStandardOutput = true,
    };
    using var process = Process.Start(psi) ?? throw new InvalidOperationException("could not start git");
    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    return output.Split('\0', StringSplitOptions.RemoveEmptyEntries).Where(f => File.Exists(Path.Combine(root, f))).Distinct().ToList();
}

static string? FindRepoRoot(string start)
{
    for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
    {
        if (Directory.Exists(Path.Combine(dir.FullName, "skills")) && Directory.Exists(Path.Combine(dir.FullName, "scripts")))
        {
            return dir.FullName;
        }
    }
    return null;
}
