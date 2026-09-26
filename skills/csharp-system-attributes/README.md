# C# System Attributes

Proactive guidance on `System.*` / BCL attributes to add while writing or reviewing C# code — the
routing table (by situation, not C# version) is in [SKILL.md](SKILL.md).

**`references/`** — one file per category, not per C# version

| File | Covers |
| --- | --- |
| `debugging-diagnostics.md` | DebuggerDisplay, DebuggerBrowsable, DebuggerTypeProxy, DebuggerStepThrough, StackTraceHidden |
| `api-lifecycle-contracts.md` | Obsolete, Conditional, EditorBrowsable |
| `nullable-flow-analysis.md` | NotNullWhen, MaybeNullWhen, MemberNotNull, MemberNotNullWhen, DoesNotReturn, DoesNotReturnIf, NotNull, MaybeNull, AllowNull, DisallowNull |
| `caller-info.md` | CallerMemberName, CallerLineNumber, CallerFilePath, CallerArgumentExpression |
| `performance.md` | MethodImpl(AggressiveInlining/AggressiveOptimization), SkipLocalsInit |
| `attribute-authoring-meta.md` | AttributeUsage, Flags |
| `analyzer-suppression.md` | SuppressMessage, UnconditionalSuppressMessage |
| `testing.md` | Verifying attribute application via reflection, testing Conditional/nullable-flow/caller-info behavior |

## Scope

BCL (`System.*`) attributes only. Out of scope: P/Invoke/marshaling attributes (`DllImport`,
`LibraryImport`, `MarshalAs`, `StructLayout`) and framework-specific attributes (ASP.NET Core,
EF Core, `System.Text.Json`, test frameworks).

Each reference file notes an attribute's version-introduced fact inline; version is not the
file-splitting axis for this skill (see [SKILL.md](SKILL.md) for why).
