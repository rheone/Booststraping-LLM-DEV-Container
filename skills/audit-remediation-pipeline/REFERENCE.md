# Audit Remediation Pipeline — Reference

## Agent Specifications

### Pedantic Developer
Focus: code correctness, edge cases, code smell, performance, safety.

Prompt template:
```
Review this code in the context of finding "{finding_desc}".
- Identify ALL code changes needed (not just the obvious one)
- Flag edge cases: nulls, empty collections, boundary values, overflow
- Flag performance concerns: allocations, boxing, high complexity, cyclomatic complexity
- Flag threading/safety concerns if relevant
- Suggest concrete code snippets for the fix
- If the fix could break callers, note the impact
Return: a structured list of required changes with file paths and line numbers.
```

### Tech Writer
Focus: documentation quality, documentation comments, code comments, terminology.

Prompt template:
```
Review documentation and comments in these files for finding "{finding_desc}".
- Check documentation comments
- Check code comments
- Check typos, grammar, tense consistency
- Check terminology matches project conventions
- Check that documentation, including readme files and code, agree
- Check TODO/FIXME/HACK markers:
  * NEVER remove or edit them — they exist for a reason
  * If the finding relates to a marker's underlying issue, try to address it
  * If context is insufficient to fix it, add a note to the batch log for user review
  * Do not assume a marker is stale or safe to remove
Return: specific fixes with file paths and line numbers. If a marker was encountered but not addressed, note it separately.
```

### Auditor
Focus: verification that fix matches finding, no regressions. Route non-code findings.

Prompt template:
```
You are the gatekeeper. Given finding "{finding_desc}" and the proposed fix:
1. Does the fix FULLY address the finding? If not, what's missing?
2. Could this fix introduce regressions? How?
3. Is the approach consistent with the rest of the codebase?
4. Are there related findings or duplicate issues in other files?
5. Is this a code change, or does it belong elsewhere? For non-code findings, return a ROUTE directive.
6. Is the finding itself AMBIGUOUS? If the description, scope, or required approach is unclear, do not guess.

Ambiguity handling:
- If the finding description is ambiguous (e.g., "improve performance" without specifics), return AMBIGUOUS with what's unclear.
- If the proposed fix path is ambiguous (multiple equally valid approaches without clear winner), return AMBIGUOUS with options listed.
- DO NOT proceed down an assumed path. The user must disambiguate.

Return one of:
- APPROVE — fix is correct, proceed to implementation
- REVISE (with specific issues) — route back to pedantic-dev (max 3 retries)
- REJECT (with reasoning) — route back to pedantic-dev (max 3 retries, escalate on overflow)
- AMBIGUOUS (with what's unclear) — flag for user intervention, add to batch log
- ROUTE: test-fix — finding is a test gap, delegate to test-generation agent
- ROUTE: documentation — finding is a doc gap, delegate to tech-writer or doc agent
- ROUTE: config — finding is a config/infra issue, escalate to user
```

### Implementer
Focus: clean, minimal, idiomatic application of the fix. Preserve existing markers.

Prompt template:
```
Apply the approved fix for finding "{finding_desc}".
- Modify ONLY the minimum necessary code
- Follow existing code style (naming, braces, spacing, etc)
- If adding docs, match the tone of existing docs
- NEVER remove or modify TODO/FIXME/HACK/obsolete markers. If they block the fix, note it in the log and stop.
- If the fix reveals an issue in commented-out or deprecated code, do not touch it. Note it in the batch log.
- After edit, verify the file compiles
Return: summary of what was changed and why. Note any markers that were encountered but left untouched.
```

### Verifier
Focus: build and test the fix.

Prompt template:
```
Verify the fix for finding "{finding_desc}".
1. Build the project
2. If multi-target, check all frameworks compile
3. Run targeted tests
4. Check for new warnings (note if pre-existing)
Return: build result, test results, and any new warnings.
```

### Re-auditor
Focus: re-evaluate all original findings against the fixed codebase.

Prompt template:
```
You are the re-auditor. Given the original findings list and the current codebase:
1. For each finding, determine if it is fully resolved
2. Classify as RESOLVED, PARTIAL (some aspects remain), or UNRESOLVED
3. Check for regressions near the changed areas
Return: a structured report with resolved/unresolved counts and any regressions.
```

### Findings Parser
Focus: normalize free-form audit reports into structured findings JSON.

Prompt template:
```
You are a findings parser. Given a free-form audit report:
1. Extract each distinct finding
2. Normalize severity: high / medium / low / info
3. Identify affected files (if mentioned)
4. Classify type: code-change / test-gap / documentation / config / infrastructure
5. Assign unique IDs (e.g., H1, M3, L7)
6. If a finding is AMBIGUOUS (unclear scope, missing files, vague description), flag it:
   - Set type to "ambiguous"
   - Include the reason in description: "AMBIGUOUS: <what's unclear>"
   - Do NOT force it into a type
Output as the Findings JSON schema:
{ "findings": [{ "id": "...", "severity": "...", "type": "...", "files": [...], "description": "..." }] }
```

## Loop Patterns

### Batch loop
```
for each batch in batches (ordered by severity):
    for each finding in batch:
        research(finding) → parallel if independent
        retries = 0
        loop:
            pedantic_dev → tech_writer
            result = auditor(proposed_fix)
            if result == APPROVE:
                implement → verify
                break
            if result == ROUTE:*:
                dispatch to appropriate handler
                break
            if result in (REVISE, REJECT):
                retries += 1
                if retries >= 3: escalate to user; break
                pedantic_dev(auditor_feedback)  # revision loop
    commit batch
```

### Fix-verify loop
```
loop:
    implement(fix)
    build
    if build fails: diagnose, fix, continue
    if tests fail: diagnose, fix, continue
    if re-audit finding: loop again with improved fix
    break when build + tests pass
```

### Re-audit loop
```
run @re-auditor(original_findings, current_codebase)
for each finding:
    if RESOLVED: log as done
    if PARTIAL: add to follow-up batch
    if UNRESOLVED: add to follow-up batch (high priority)
if follow-up batch exists: loop back to Phase 2
```

## Findings Schema (Structured Input)

```json
{
  "findings": [
    {
      "id": "H2",
      "severity": "high",
      "type": "code-change",
      "files": ["src/Arcus/AbstractIPAddressRange.cs"],
      "description": "GetHashCode is inconsistent across subtypes; unified HashCode.Combine approach needed"
    }
  ]
}
```

Fields:
- `id` — unique identifier (e.g., H1, M3, L7, I2)
- `severity` — `high` / `medium` / `low` / `info`
- `type` — `code-change` / `test-gap` / `documentation` / `config` / `infrastructure`
- `files` — array of affected file paths (optional, may be empty for cross-cutting findings)
- `description` — what the finding is and what fix is needed
- `source` — original text (preserved when parsed from free-form)

## Pipeline State Schema (Crash Resume)

`pipeline-state.json`:

```json
{
  "branch": "pilot/batch-2",
  "started_at": "2026-06-12T10:30:00Z",
  "batches": [
    {
      "id": 1,
      "status": "completed",
      "finding_ids": ["H2", "H3", "H4"],
      "commit": "e5bb4f6"
    },
    {
      "id": 2,
      "status": "in_progress",
      "finding_ids": ["H5", "H6"],
      "current_finding": "H6",
      "current_stage": "tech-writer",
      "retries": 1,
      "last_auditor_output": "REVISE: missing edge case for null input"
    }
  ]
}
```

On resume: orchestrator reads this file, skips `completed` batches, restarts `current_finding` at `current_stage`, and passes `last_auditor_output` context.

## Verification Checklist

After each fix, verify:
- [ ] Project compiles on all targets
- [ ] No new warnings in changed files
- [ ] Targeted tests pass (at minimum)
- [ ] Code is formatted consistently with project style

## Log Format

Each batch should produce a `batch-N-log.md`:

```markdown
# Batch N Audit Log

## Scope
[List of findings in this batch]

## Files Changed
| Finding | File(s) | Change |
|---------|---------|--------|
| H1 | `foo.cs` | Description |

## Build & Test Results
- Project builds? Y/N (warnings?)
- Tests: X/Y passed

## Decisions
- Any alternative approaches considered and rejected
- Rationale for chosen approach
```

## Agent Configuration

To register all pipeline agents in your project, add to `opencode.jsonc` or available tooling alternative:

```jsonc
{
  "$schema": "https://opencode.ai/config.json",
  "agent": {
    "audit-orchestrator": {
      "description": "Orchestrates the audit remediation pipeline across pedantic-dev, tech-writer, auditor, implementer, and verifier sub-agents",
      "mode": "all",
      "permission": {
        "task": {
          "audit-orchestrator": "allow",
          "pedantic-dev": "allow",
          "tech-writer": "allow",
          "auditor": "allow",
          "implementer": "allow",
          "verifier": "allow"
        },
        "edit": "allow",
        "bash": "allow",
        "read": "allow",
        "glob": "allow",
        "grep": "allow",
        "skill": "allow"
      }
    },
    "pedantic-dev": {
      "description": "Reviews code for correctness, edge cases, performance, and safety",
      "mode": "subagent",
      "permission": {
        "read": "allow",
        "grep": "allow",
        "glob": "allow",
        "bash": { "*": "ask", "git diff*": "allow", "git log*": "allow", "grep *": "allow" },
        "edit": "deny",
        "task": "deny",
        "webfetch": "deny",
        "websearch": "deny"
      }
    },
    "tech-writer": {
      "description": "Reviews documentation, XML comments, cref values, typos, terminology",
      "mode": "subagent",
      "permission": {
        "read": "allow",
        "grep": "allow",
        "glob": "allow",
        "bash": { "*": "ask", "git diff*": "allow", "git log*": "allow", "grep *": "allow" },
        "edit": "deny",
        "task": "deny",
        "webfetch": "deny",
        "websearch": "deny"
      }
    },
    "auditor": {
      "description": "Gatekeeper — verifies fixes fully address findings without regressions. Approves/revises/rejects.",
      "mode": "subagent",
      "permission": {
        "read": "allow",
        "grep": "allow",
        "glob": "allow",
        "bash": { "*": "ask", "git diff*": "allow", "git log*": "allow", "grep *": "allow" },
        "edit": "deny",
        "task": "deny",
        "webfetch": "deny",
        "websearch": "deny"
      }
    },
    "implementer": {
      "description": "Applies approved audit fixes to code. Edits files per the approved specification.",
      "mode": "subagent",
      "permission": {
        "edit": "allow",
        "read": "allow",
        "bash": "allow",
        "grep": "allow",
        "glob": "allow",
        "task": "deny",
        "webfetch": "deny",
        "websearch": "deny"
      }
    },
    "verifier": {
      "description": "Builds and tests the codebase after fixes. Reports compilation and test results.",
      "mode": "subagent",
      "permission": {
        "bash": "allow",
        "read": "allow",
        "grep": "allow",
        "glob": "allow",
        "edit": "deny",
        "task": "deny",
        "webfetch": "deny",
        "websearch": "deny"
      }
    },
    "re-auditor": {
      "description": "Re-evaluates original findings against fixed code. Reports resolved/partial/unresolved statuses.",
      "mode": "subagent",
      "permission": {
        "read": "allow",
        "grep": "allow",
        "glob": "allow",
        "bash": { "*": "ask", "git diff*": "allow", "git log*": "allow", "grep *": "allow" },
        "edit": "deny",
        "task": "deny"
      }
    },
    "findings-parser": {
      "description": "Parses free-form audit reports into structured findings JSON. Normalizes severity and classifies types.",
      "mode": "subagent",
      "permission": {
        "read": "allow",
        "grep": "allow",
        "edit": "deny",
        "bash": "deny",
        "task": "deny"
      }
    }
  }
}
```

### Installing as markdown agents

Alternatively, place the `.md` files from `agents/` into:

- **Global**: `~/.config/opencode/agents/`
- **Per-project**: `.opencode/agents/`

Each filename (without `.md`) becomes the agent name. The YAML frontmatter defines permissions, the body is the system prompt.

## Non-Code Finding Routing

When the auditor returns a `ROUTE:` directive instead of APPROVE/REVISE/REJECT, the orchestrator dispatches to the appropriate handler:

| ROUTE target | Handler | Example finding type |
|---|---|---|
| `ROUTE: test-fix` | `@code-testing-agent` or test-generation skill | Missing coverage, uncovered edge cases, weak assertions |
| `ROUTE: documentation` | `@tech-writer` or doc-generation skill | Missing README, API docs, inline comments |
| `ROUTE: config` | User/infra skill | Build config, CI, .runsettings, package versions |

The finding is marked as `dispatched` in pipeline-state.json and tracked separately from the code-change findings.

## Common Pitfalls

1. **Multi-target LSP false errors**: LSP may show errors for TFMs not currently targeted by the language server. Trust the build, not the LSP.
1. **Full test suite timeout**: For large suites (>5000 tests), run targeted subsets during iteration. Full suite only at final verification.
1. **Pre-existing working tree changes**: Always check `git diff --stat HEAD` before starting to avoid conflating pre-existing changes with fix changes.

## Example Session Flow

```
Agent: Loads audit-remediation-pipeline skill, checks git status
Agent: Asks user for findings → user provides free-form report
Agent: Runs @findings-parser → structured JSON output
Agent: Plans batches: Batch 1 = H2-H4 (IIPAddressRange files, high overlap → sequential)
Agent: Creates branch pilot/batch-1, initializes pipeline-state.json
Agent: Batch 1 processing:
  → @explore: "H2: GetHashCode inconsistent across subtypes"
  → @pedantic-dev: "Suggests HashCode.Combine(Head,Tail) approach"
  → @tech-writer: "Checks XML docs, cref values on all 3 files"
  → @auditor: "APPROVE"
  → @implementer: "Edits AbstractIPAddressRange.cs, Subnet.IEquatable.cs, IPAddressRange.IEquatable.cs"
  → @verifier: "Builds + targeted tests → PASS"
  → Updates pipeline-state.json (H2 completed)
  → ... (H3, H4 same pipeline)
Agent: Commits batch, runs @re-auditor → all 3 RESOLVED
Agent: Proceeds to Batch 2
```

## Tool invocation reference

When orchestrating pipeline stages, invoke sub-agents using the `task` tool with context from the previous stage:

```
task → @pedantic-dev with: "Finding H2: GetHashCode inconsistent across subtypes.
Files: AbstractIPAddressRange.cs, Subnet.IEquatable.cs, IPAddressRange.IEquatable.cs
Research: {output from @explore stage}"
```

Each stage receives the **finding description** + **all previous stage outputs** so it has full context. This avoids repeating research while keeping each agent focused on its specific role.
