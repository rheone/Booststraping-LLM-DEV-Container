#!/usr/bin/env python3
"""
detect_n_plus_one.py — heuristic scan for the loop + lazy-nav-access shape that causes N+1
query patterns. See reference/lazy-loading-and-fetching.md § N+1 Queries.

STATUS: MVP scaffold. Catches the most common syntactic shape (foreach over a query result,
with a nested property access inside the loop body). Does not understand whether the accessed
property is actually lazy-mapped (would require parsing the corresponding *Map.cs) — cross-
reference findings against the entity's mapping file by hand, or extend this script to parse
mapping files the way detect_cascade_misconfig.py does, sharing that parsing logic.

Usage:
    python detect_n_plus_one.py <path-to-file-or-directory>
"""

import argparse
import re
import sys
from pathlib import Path

FOREACH_RE = re.compile(r"foreach\s*\(\s*var\s+(\w+)\s+in\s+")
NESTED_ACCESS_RE = re.compile(r"\b(\w+)\.(\w+)\.(\w+)\b")


def scan_file(path: Path):
    findings = []
    try:
        lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
    except Exception as e:
        return [f"{path}: could not read ({e})"]

    in_loop_var = None
    loop_start_depth = None
    depth = 0

    for i, line in enumerate(lines, start=1):
        m = FOREACH_RE.search(line)
        if m and in_loop_var is None:
            in_loop_var = m.group(1)
            loop_start_depth = depth

        depth += line.count("{") - line.count("}")

        if in_loop_var and loop_start_depth is not None and depth <= loop_start_depth and i > 1:
            # left the loop body (rough heuristic)
            in_loop_var = None
            loop_start_depth = None
            continue

        if in_loop_var:
            for var, nav, prop in NESTED_ACCESS_RE.findall(line):
                if var == in_loop_var:
                    findings.append(
                        f"{path}:{i}: '{var}.{nav}.{prop}' accessed inside foreach over '{var}' — "
                        f"check whether '{nav}' is lazy-mapped on the loop element's type; if so, this is likely N+1"
                    )

    return findings


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("target")
    args = parser.parse_args()

    target = Path(args.target)
    files = [target] if target.is_file() else list(target.rglob("*.cs"))

    all_findings = []
    for f in files:
        all_findings.extend(scan_file(f))

    if not all_findings:
        print("No obvious foreach + nested-access candidates found. Heuristic only — "
              "doesn't catch N+1 via LINQ .Select()/.Sum() chains over lazy collections; review those by hand.")
        return 0

    print(f"{len(all_findings)} candidate(s) — cross-check each against the entity's mapping file:\n")
    for finding in all_findings:
        print(f"  {finding}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
