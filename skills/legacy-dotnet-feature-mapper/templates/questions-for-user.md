---
last_updated: <date>
counts:
  open: 0
  answered: 0
  applied: 0
  blocking_open: 0
---
# Questions for the User

Anything the run could not resolve on its own. An unattended run files a
question here and moves on rather than stopping (see
`references/questions-and-deferral.md`).

**To answer:** fill in the `Answer:` line and change `Status:` from `open` to
`answered`. The next session reconciles this file before doing anything else -
answers are applied to the affected docs and the status becomes `applied`.

**Blocking vs non-blocking:** a *blocking* question invalidated real analysis,
so answering it re-opens the affected feature for re-derivation. A
*non-blocking* answer is patched into the existing doc in place.

---

## qst-XXXX | <short question title>

- **Status:** open
- **Blocking:** yes | no
- **Feature(s):** feat-XXXX
- **Raised:** <ISO-8601>
- **Context:** <what was being traced when this came up, with citation>

**Question:** <the question, stated so it can be answered without re-reading the code>

**Options:**
- **A)** <option>
- **B)** <option>
- **C)** Something else - please describe

**Answer:** <leave blank until answered>

**Applied:** <ISO-8601 when folded back into the docs, or blank>

---
