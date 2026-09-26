# C# Visitor Pattern

Reference for the Visitor design pattern in C#: the classic double-dispatch form, a generic
`IVisitor<TResult>` returning a typed result, the extension tension between adding operations and
adding visited types, and `switch`-expression pattern matching as a lighter alternative for closed
hierarchies. The routing table is in [SKILL.md](SKILL.md).

**`references/`**

| File | Covers |
| --- | --- |
| `classic-double-dispatch-visitor.md` | `Accept`/`Visit`, why `this` must be statically typed as the concrete class, `void`-returning visitors |
| `generic-visitor-typed-result.md` | `IVisitor<TResult>`, a generic `Accept<TResult>`, combining `void` and typed visitors over one hierarchy |
| `adding-visited-types-vs-adding-visitors.md` | why new operations are free and new visited types force every visitor to change; choosing the hierarchy shape up front |
| `pattern-matching-alternative.md` | `switch` expressions over a sealed hierarchy; when pattern matching is enough and when it isn't |
| `testing-visitors.md` | testing a visitor's computed result, testing `Accept` dispatch correctness, testing state-accumulating visitors |
| `extending-visitors.md` | adding a new visitor without touching the hierarchy; keeping visitors independent of each other |
