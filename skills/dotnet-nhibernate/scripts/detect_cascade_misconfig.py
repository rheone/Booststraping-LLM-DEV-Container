#!/usr/bin/env python3
"""
detect_cascade_misconfig.py — scans a directory of Fluent NHibernate mapping files for
Inverse()/cascade configuration that looks mismatched between the two sides of a
bidirectional relationship.

See reference/cascade-and-relationships.md for the underlying rule this checks:
exactly one side of a HasMany <-> References pair should be marked .Inverse() (the "one" side),
and neither/both being marked is a common source of duplicate-insert or silent-save-failure bugs.

This is a heuristic, regex-based scan across *Map.cs files. It pattern-matches KeyColumn/Column
names to pair up HasMany(...) on one entity with References(...) on another — it will miss pairs
where naming conventions aren't followed, and can misfire on genuinely one-directional
relationships (a HasMany with no corresponding References on the child at all, which is valid
and not a bug). Review every finding; don't treat this as authoritative.

Usage:
    python detect_cascade_misconfig.py <path-to-mappings-directory>
"""

import argparse
import re
import sys
from pathlib import Path

CLASS_MAP_RE = re.compile(r"class\s+(\w+)Map\s*:\s*ClassMap<(\w+)>")
HAS_MANY_RE = re.compile(
    r"HasMany\(x\s*=>\s*x\.(\w+)\)"
    r"(?:\s*\.\s*\w+\([^)]*\))*?"  # any chained calls before we check for specific ones
    , re.DOTALL
)
KEY_COLUMN_RE = re.compile(r"\.KeyColumn\(\s*\"([^\"]+)\"\s*\)")
INVERSE_RE = re.compile(r"\.Inverse\(\)")
REFERENCES_RE = re.compile(
    r"References\(x\s*=>\s*x\.(\w+)\)(?:\s*\.\s*\w+\([^)]*\))*?\.Column\(\s*\"([^\"]+)\"\s*\)",
    re.DOTALL,
)


def find_hasmany_blocks(text):
    """Return list of (start_index, block_text) for each HasMany(...) chain, roughly to the next semicolon."""
    blocks = []
    for m in re.finditer(r"HasMany\(x\s*=>\s*x\.\w+\)", text):
        start = m.start()
        end = text.find(";", start)
        if end == -1:
            end = len(text)
        blocks.append(text[start:end + 1])
    return blocks


def find_references_blocks(text):
    blocks = []
    for m in re.finditer(r"References\(x\s*=>\s*x\.\w+\)", text):
        start = m.start()
        end = text.find(";", start)
        if end == -1:
            end = len(text)
        blocks.append(text[start:end + 1])
    return blocks


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("target", help="Directory containing *Map.cs files")
    args = parser.parse_args()

    target = Path(args.target)
    map_files = list(target.rglob("*Map.cs"))
    if not map_files:
        print(f"No *Map.cs files found under {target}")
        return 1

    # collect: key_column -> {'hasmany': [(file, has_inverse)], 'references': [(file, is_child_side)]}
    key_column_index = {}

    for f in map_files:
        text = f.read_text(encoding="utf-8", errors="ignore")

        for block in find_hasmany_blocks(text):
            key_match = KEY_COLUMN_RE.search(block)
            if not key_match:
                continue
            key_col = key_match.group(1)
            has_inverse = bool(INVERSE_RE.search(block))
            key_column_index.setdefault(key_col, {}).setdefault("hasmany", []).append((f, has_inverse))

        for block in find_references_blocks(text):
            col_match = re.search(r"\.Column\(\s*\"([^\"]+)\"\s*\)", block)
            if not col_match:
                continue
            key_col = col_match.group(1)
            has_inverse = bool(INVERSE_RE.search(block))  # Inverse() on References is itself unusual
            key_column_index.setdefault(key_col, {}).setdefault("references", []).append((f, has_inverse))

    findings = []
    for key_col, sides in key_column_index.items():
        hasmany_sides = sides.get("hasmany", [])
        references_sides = sides.get("references", [])
        if not hasmany_sides or not references_sides:
            continue  # only one side found in this scan — not necessarily a bug, could be unidirectional

        hasmany_inverse_count = sum(1 for _, inv in hasmany_sides if inv)
        references_inverse_count = sum(1 for _, inv in references_sides if inv)

        if hasmany_inverse_count == 0:
            findings.append(
                f"KeyColumn '{key_col}': HasMany side(s) [{', '.join(str(f) for f, _ in hasmany_sides)}] "
                f"have NO .Inverse() — likely both sides managing the FK, expect duplicate/extra UPDATE statements"
            )
        if references_inverse_count > 0:
            findings.append(
                f"KeyColumn '{key_col}': References side(s) [{', '.join(str(f) for f, _ in references_sides)}] "
                f"unexpectedly have .Inverse() — the child/References side owning the FK should NOT be marked Inverse; "
                f"if BOTH sides have it, the relationship likely fails to persist at all"
            )

    if not findings:
        print(f"Scanned {len(map_files)} mapping file(s). No obvious Inverse()/cascade mismatches found.\n"
              "This does not guarantee correctness — review cascade CHOICE (not just Inverse placement) "
              "by hand against reference/cascade-and-relationships.md.")
        return 0

    print(f"Scanned {len(map_files)} mapping file(s). {len(findings)} possible issue(s):\n")
    for finding in findings:
        print(f"  - {finding}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
