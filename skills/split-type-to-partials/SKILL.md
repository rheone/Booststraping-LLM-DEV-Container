---
name: split-type-to-partials
description: Refactors a C# type (class, record, struct) into partial files split by implemented interfaces and functional groupings (Factory, Operators). Mirrors the split in the corresponding test file. Updates the .csproj with DependentUpon nesting for all partials. Use when asked to "split into partials", "refactor by interface", "break up a C# type", or "add partial files" for a C#/.NET type.
author: Robert Engelhardt <rheone@gmail.com>
version: 1.0.0
---

# split-type-to-partials

C#/.NET only. Requires the type to directly implement or extend more than one type (interface or base class).

## Quick start

Invoke with no args — the skill infers the root type from IDE context (the open/selected file whose name matches `{Root}.cs` without a `{Root}.{Something}.cs` pattern). If inference fails, ask the user for the file path.

## Workflow

### 1. Infer & validate
- Identify the root `.cs` file and parse the type declaration
- Confirm the type implements/extends **more than one** type — abort with a clear message if not
- Determine scope: **both** type and tests by default; narrow only if user explicitly asks for type-only or tests-only

### 2. Classify members
Apply rules from [REFERENCE.md](REFERENCE.md#member-classification) to every member:
- Interface method/property → `{Type}.{BaseInterfaceName}.cs`
- Static factory method → `{Type}.Factory.cs`
- Operator → `{Type}.Operators.cs`
- Constructor stays in root
- Everything else stays in root (fields, properties, private helpers, nested types, etc.)

Group interfaces that share a base name (ignoring generic arity) into a single partial (e.g., `IComparable<T>` + `IComparable` → `{Type}.IComparable.cs`).

### 3. Locate tests
- Search convention: find `{TypeName}Tests.cs` by walking up to the `.sln` then searching sibling test projects
- Multiple matches → ask user to disambiguate
- No match → create root test file + partial skeletons
- Classify existing test methods via two-pass heuristic: method name contains an interface method name, then body scan for calls — ambiguous tests stay in root with `// TODO: manually move to appropriate test partial`

### 4. Show plan & confirm
Present a full summary before touching any file:
- Files to create (source + test partials)
- Members moving to each partial
- Test methods moving to each test partial (and which stay with TODO)
- `.csproj` entries to add

**Do not proceed until the user confirms.**

### 5. Execute
- Write/update source partial files (see [REFERENCE.md](REFERENCE.md#partial-file-template))
- Write/update test partial files (see [REFERENCE.md](REFERENCE.md#test-partial-template))
- Update source `.csproj` with `DependentUpon` entries under root type
- Update test `.csproj` with `DependentUpon` entries under `{TypeName}Tests.cs`

## Update mode (existing partials)
When partials already exist:
- Create missing partials for newly added interfaces; move the new members
- Add/repair missing `DependentUpon` entries in `.csproj` without touching source files
- **Never** re-sort members between existing partials unless explicitly asked to "reclassify all members" — in that case, treat as normal execution but with an extra confirmation step highlighting all members that will move between existing partials
