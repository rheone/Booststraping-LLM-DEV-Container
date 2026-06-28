# Skills Catalog

Agentic AI skills for C# / .NET development, designed for use with [Claude Code](https://docs.anthropic.com/en/docs/claude-code/overview) and [OpenCode](https://opencode.ai/).

---

## Installation

Install every skill in the repository:

```bash
npx skills add rheone/Booststraping-LLM-DEV-Container
```

Install specific skills with the `--skill` flag:

```bash
# Install just the test sweep and xUnit companion
npx skills add rheone/Booststraping-LLM-DEV-Container --skill csharp-test-sweep --skill xunit-csharp

# Install documentation and refactoring skills
npx skills add rheone/Booststraping-LLM-DEV-Container --skill reverse-engineered-docs --skill csharp-split-type-to-partials
```

Install from a local path:

```bash
npx skills add /path/to/llm-dev-container
```

### Manage installed skills

```bash
# See what's installed and active
npx skills list

# Enable / disable selectively
npx skills enable csharp-test-sweep reverse-engineered-docs
npx skills disable audit-remediation-pipeline
```

---

## Skill Catalog

### Code Review & Remediation

| Skill | Description | Install |
|-------|-------------|---------|
| [`audit-remediation-pipeline`](skills/audit-remediation-pipeline) | Systematic multi-agent pipeline for audit findings — research → pedantic review → tech writer → auditor → implement → verify | `--skill audit-remediation-pipeline` |

### Documentation

| Skill | Description | Install |
|-------|-------------|---------|
| [`csharp-docs-and-comments`](skills/csharp-docs-and-comments) | Add/improve XML doc comments and inline comments in C# codebases | `--skill csharp-docs-and-comments` |
| [`reverse-engineered-docs`](skills/reverse-engineered-docs) | Reverse-engineer source code into structured markdown docs with confidence annotations | `--skill reverse-engineered-docs` |

### Refactoring

| Skill | Description | Install |
|-------|-------------|---------|
| [`csharp-split-type-to-partials`](skills/csharp-split-type-to-partials) | Split C# types into partial files by interface/functional grouping | `--skill csharp-split-type-to-partials` |
| [`csharp-library-repo-structure`](skills/csharp-library-repo-structure) | Bootstrap, audit, and refactor .NET library repo layout for NuGet distribution | `--skill csharp-library-repo-structure` |

### Test Suite Sweep (Orchestrator)

| Skill | Description | Install |
|-------|-------------|---------|
| [`csharp-test-sweep`](skills/csharp-test-sweep) | Orchestrates project-wide test suite improvement — detects framework/mocking library, runs 16-step discovery, dispatches to companion skills, iterates each class with verification | `--skill csharp-test-sweep` |

### Test Frameworks (Companion)

| Skill | Description | Install |
|-------|-------------|---------|
| [`xunit-csharp`](skills/csharp-test-sweep/skills/xunit-csharp) | xUnit v3 rules — `[Fact]`/`[Theory]`, `TheoryData<T>`, `Assert.Equivalent`, fixtures | `--skill xunit-csharp` |
| [`nunit-csharp`](skills/csharp-test-sweep/skills/nunit-csharp) | NUnit v5 rules — constraint-based assertions, `[TestCase]`, `[Retry]`, parallelization | `--skill nunit-csharp` |
| [`mstest-csharp`](skills/csharp-test-sweep/skills/mstest-csharp) | MSTest v4 rules — `[DataRow]`/`[DynamicData]`, `CollectionAssert`, lifecycle attributes | `--skill mstest-csharp` |

### Mocking Libraries (Companion)

| Skill | Description | Install |
|-------|-------------|---------|
| [`moq-csharp`](skills/csharp-test-sweep/skills/moq-csharp) | Moq 4.x — `MockBehavior`, `.Setup().Returns()`, argument matchers, verification | `--skill moq-csharp` |
| [`nsubstitute-csharp`](skills/csharp-test-sweep/skills/nsubstitute-csharp) | NSubstitute — `Substitute.For<T>()`, `Arg.Is<T>()`, `Received()` verification | `--skill nsubstitute-csharp` |
| [`justmock-csharp`](skills/csharp-test-sweep/skills/justmock-csharp) | Telerik JustMock — free/elevated mode, non-virtual/static interception in elevated | `--skill justmock-csharp` |
| [`rhinomocks-csharp`](skills/csharp-test-sweep/skills/rhinomocks-csharp) | RhinoMocks — legacy suite maintenance, AAA style, migration pathway to NSubstitute/Moq | `--skill rhinomocks-csharp` |

---

## Companion Skill Hierarchy

The companion skills (test frameworks + mocking libraries) live under `skills/csharp-test-sweep/skills/`. They are invoked automatically by [`csharp-test-sweep`](skills/csharp-test-sweep) and can also be installed standalone via their skill name.
