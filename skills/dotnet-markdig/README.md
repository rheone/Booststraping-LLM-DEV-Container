# Markdig

Guidance on Markdig, the CommonMark-compliant Markdown processor for .NET — the routing table (by
task, not Markdig version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per topic, not per Markdig version

| File | Covers |
| --- | --- |
| `pipeline-configuration.md` | `MarkdownPipelineBuilder`, `UseAdvancedExtensions()`, opting into extensions individually, `Build()` |
| `common-extensions.md` | Pipe tables, task lists, auto-links, YAML frontmatter |
| `rendering-html-and-text.md` | `Markdown.ToHtml`, `HtmlRenderer`, `Markdown.ToPlainText` |
| `custom-extensions.md` | `IMarkdownExtension`, custom block/inline parsers and renderers |
| `ast-manipulation.md` | `MarkdownDocument`, `Descendants<T>()`, walking and mutating the parsed tree |
| `testing.md` | Testing Markdown-to-HTML/text conversions and custom extensions |

## Scope

Markdig only — parsing and rendering Markdown text in .NET. Out of scope: editor/preview UI
controls, fenced-code syntax highlighting beyond the CSS class Markdig emits, and math/LaTeX
rendering (no built-in Markdig extension covers it).

Each reference file notes a version-sensitive fact inline (e.g. which extensions
`UseAdvancedExtensions()` does and doesn't include); version is not the file-splitting axis for
this skill (see [SKILL.md](SKILL.md) for why).
