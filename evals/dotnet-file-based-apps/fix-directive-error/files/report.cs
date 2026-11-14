#:package Spectre.Console
#:property LangVersion preview
using Spectre.Console;

var table = new Table().AddColumns("Name", "Size");
foreach (var f in new DirectoryInfo(".").GetFiles())
    table.AddRow(f.Name, f.Length.ToString());
AnsiConsole.Write(table);
