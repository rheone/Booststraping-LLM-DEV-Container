#!/usr/bin/env python3
"""
detect_lazy_after_close.py — heuristic scan for lazy NHibernate property/collection access
that occurs after the owning session is likely to have closed.

This is a static, regex-based heuristic, NOT a guaranteed analysis (that would require real
Roslyn semantic analysis of session lifetime, which is a larger project). Treat every hit as a
prompt to look closer by hand, not a verdict — false positives and false negatives are both
expected. See reference/lazy-loading-and-fetching.md for the underlying concept.

Usage:
    python detect_lazy_after_close.py <path-to-file-or-directory> [--session-var-names session,ISession]

What it flags:
    - A method that returns an entity (or takes one as a parameter) loaded from a repository
      method, where the calling method's own scope appears to end (return statement, or a
      `using` block around a session/scope closes) before some later code path plausibly
      touches a nested property (`entity.Foo.Bar` — two or more dots, a common shape for
      touching a lazy nav property).
    - `using (var session = ...)` blocks where an entity obtained inside are referenced by
      name after the closing brace of that `using` block, in the same file.

This is intentionally conservative in scope (single-file, syntactic) — it will not catch
cross-file/cross-layer boundary issues, which are the more common real-world case. For those,
this script is a starting point to sanity check obvious local cases; rely on the session
lifecycle review checklist in reference/session-lifecycle.md for the rest.
"""

import argparse
import re
import sys
from pathlib import Path

USING_SESSION_RE = re.compile(r"using\s*\(\s*var\s+(\w+)\s*=\s*[^)]*\.(OpenSession|BeginTransaction)\s*\(")
NESTED_ACCESS_RE = re.compile(r"\b(\w+)\.(\w+)\.(\w+)\b")  # entity.Nav.Prop shape


def scan_file(path: Path):
    findings = []
    try:
        text = path.read_text(encoding="utf-8", errors="ignore")
    except Exception as e:
        return [f"{path}: could not read ({e})"]

    lines = text.splitlines()
    open_session_vars = {}  # var name -> line number opened

    depth = 0
    session_close_depth = {}

    for i, line in enumerate(lines, start=1):
        m = USING_SESSION_RE.search(line)
        if m:
            var_name = m.group(1)
            open_session_vars[var_name] = i
            session_close_depth[var_name] = depth
        depth += line.count("{") - line.count("}")

        # crude: once we've dropped back to or below the depth the using block opened at,
        # consider that session var "closed" for subsequent lines
        closed_now = [v for v, d in session_close_depth.items() if depth <= d and i > open_session_vars[v]]
        for v in closed_now:
            del session_close_depth[v]

        nested = NESTED_ACCESS_RE.findall(line)
        for entity_var, nav, prop in nested:
            if entity_var in open_session_vars and entity_var not in session_close_depth:
                findings.append(
                    f"{path}:{i}: possible lazy access on '{entity_var}.{nav}.{prop}' "
                    f"after session '{entity_var}' (opened line {open_session_vars[entity_var]}) went out of scope — verify manually"
                )

    return findings


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("target", help="File or directory to scan")
    args = parser.parse_args()

    target = Path(args.target)
    files = [target] if target.is_file() else list(target.rglob("*.cs"))

    all_findings = []
    for f in files:
        all_findings.extend(scan_file(f))

    if not all_findings:
        print("No obvious candidates found. This does NOT mean the code is safe — "
              "this heuristic only catches simple single-file, syntactic cases. "
              "See reference/lazy-loading-and-fetching.md for the full picture.")
        return 0

    print(f"{len(all_findings)} candidate(s) found — review each by hand, this is a heuristic:\n")
    for finding in all_findings:
        print(f"  {finding}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
