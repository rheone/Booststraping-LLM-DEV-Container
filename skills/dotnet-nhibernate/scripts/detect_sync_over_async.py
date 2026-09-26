#!/usr/bin/env python3
"""
detect_sync_over_async.py — heuristic scan for sync-over-async blocking calls
(.Result, .Wait(), .GetAwaiter().GetResult()) on expressions that look like NHibernate
session/query calls. See reference/async-patterns.md.

STATUS: MVP scaffold, regex-based. It flags the pattern generally and additionally raises
confidence when the expression contains NHibernate-ish tokens (session, Query<, SaveAsync,
ToListAsync, etc.) — but it can't tell for certain whether a given `.Result` is on an
NHibernate call or some unrelated Task. Review every hit.

Usage:
    python detect_sync_over_async.py <path-to-file-or-directory>
"""

import argparse
import re
import sys
from pathlib import Path

BLOCKING_RE = re.compile(r"\.Result\b|\.Wait\(\s*\)|\.GetAwaiter\(\)\.GetResult\(\)")
NHIBERNATE_HINT_RE = re.compile(
    r"\b(session|Session|ISession|Query<|QueryOver<|SaveAsync|UpdateAsync|DeleteAsync|"
    r"ToListAsync|ToFutureValue|GetAsync|CommitAsync|RollbackAsync|StatelessSession)\b"
)
ASYNC_VOID_RE = re.compile(r"\basync\s+void\s+\w+\s*\(")


def scan_file(path: Path):
    findings = []
    try:
        lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
    except Exception as e:
        return [f"{path}: could not read ({e})"]

    for i, line in enumerate(lines, start=1):
        if BLOCKING_RE.search(line):
            confidence = "HIGH" if NHIBERNATE_HINT_RE.search(line) else "LOW (no NHibernate token on this line — check context)"
            findings.append(f"{path}:{i}: sync-over-async blocking call — confidence {confidence}\n      {line.strip()}")

        if ASYNC_VOID_RE.search(line):
            findings.append(
                f"{path}:{i}: 'async void' method — should almost always be 'async Task' "
                f"if it touches NHibernate (exceptions inside async void are silently lost)\n      {line.strip()}"
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
        print("No sync-over-async or async-void candidates found.")
        return 0

    print(f"{len(all_findings)} candidate(s) — review each, especially LOW-confidence ones:\n")
    for finding in all_findings:
        print(f"  {finding}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
