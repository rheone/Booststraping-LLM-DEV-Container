#!/usr/bin/env python3
"""Validate skill structure, frontmatter, and insularity across skills/.

Two checks:
1. Structure/frontmatter: every skills/<dir>/SKILL.md has valid YAML frontmatter
   with the required fields, and its `name:` matches the directory name exactly.
   Every skill directory also has a README.md.
2. Insularity: a skill's own files must not name a sibling skill's directory
   slug. csharp-skill-authoring (the meta-skill, which documents the naming
   convention using real skill names as worked examples) and csharp-test-sweep
   (the orchestrator, which legitimately names its own nested companions) are
   excluded from the insularity check.

Exits non-zero and prints every violation if any check fails.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

import yaml

REPO_ROOT = Path(__file__).resolve().parent.parent
SKILLS_DIR = REPO_ROOT / "skills"

REQUIRED_FRONTMATTER_FIELDS = ["name", "description", "license"]

INSULARITY_EXEMPT_SKILLS = {"csharp-skill-authoring", "csharp-test-sweep"}

# Skills that predate this catalog's conventions (already merged to main before the
# naming/structure/testability standard existed) or that aren't part of the C#/.NET
# skill catalog at all. Out of scope for this validator until someone deliberately
# brings them up to the current standard.
NOT_YET_CONFORMING = {
    "audit-remediation-pipeline",
    "csharp-code-organization",
    "csharp-docs-and-comments",
    "csharp-library-repo-structure",
    "csharp-split-type-to-partials",
    "csharp-test-sweep",
    "csharp-union",
    "dispatch-tasks",
    "dotnet-nhibernate",
    "github-markdown",
    "legacy-dotnet-feature-mapper",
    "mermaid-diagram-generator",
    "reverse-engineered-docs",
}


def find_skill_dirs() -> list[Path]:
    """Every directory anywhere under skills/ that has its own SKILL.md, excluding
    skills not yet held to this catalog's conventions."""
    return sorted(
        p.parent
        for p in SKILLS_DIR.rglob("SKILL.md")
        if p.parent.relative_to(SKILLS_DIR).parts[0] not in NOT_YET_CONFORMING
    )


def parse_frontmatter(skill_md: Path) -> dict | None:
    text = skill_md.read_text(encoding="utf-8")
    if not text.startswith("---"):
        return None
    parts = text.split("---", 2)
    if len(parts) < 3:
        return None
    try:
        return yaml.safe_load(parts[1]) or {}
    except yaml.YAMLError:
        return None


def check_structure(skill_dirs: list[Path]) -> list[str]:
    errors = []
    for skill_dir in skill_dirs:
        skill_md = skill_dir / "SKILL.md"
        rel = skill_md.relative_to(REPO_ROOT)

        frontmatter = parse_frontmatter(skill_md)
        if frontmatter is None:
            errors.append(f"{rel}: missing or unparsable YAML frontmatter")
            continue

        for field in REQUIRED_FRONTMATTER_FIELDS:
            if field not in frontmatter:
                errors.append(f"{rel}: frontmatter is missing required field '{field}'")

        declared_name = frontmatter.get("name")
        if declared_name is not None and declared_name != skill_dir.name:
            errors.append(
                f"{rel}: frontmatter name '{declared_name}' does not match "
                f"directory name '{skill_dir.name}'"
            )

        if not (skill_dir / "README.md").exists():
            errors.append(f"{skill_dir.relative_to(REPO_ROOT)}: missing README.md")

    return errors


def check_insularity(skill_dirs: list[Path]) -> list[str]:
    all_slugs = {p.name for p in skill_dirs}
    errors = []

    for skill_dir in skill_dirs:
        if skill_dir.name in INSULARITY_EXEMPT_SKILLS:
            continue
        # A companion skill nested under an exempt orchestrator's own skills/
        # directory is part of that orchestrator's exempt structure.
        if any(part in INSULARITY_EXEMPT_SKILLS for part in skill_dir.parts):
            continue

        other_slugs = all_slugs - {skill_dir.name}
        if not other_slugs:
            continue
        pattern = re.compile(
            r"(?<![\w-])(" + "|".join(re.escape(s) for s in other_slugs) + r")(?![\w-])"
        )

        for md_file in skill_dir.rglob("*.md"):
            text = md_file.read_text(encoding="utf-8", errors="ignore")
            for match in pattern.finditer(text):
                line_no = text.count("\n", 0, match.start()) + 1
                errors.append(
                    f"{md_file.relative_to(REPO_ROOT)}:{line_no}: names sibling "
                    f"skill '{match.group(1)}' — skills must be insular "
                    f"(see csharp-skill-authoring/references/insularity.md)"
                )

    return errors


def main() -> int:
    skill_dirs = find_skill_dirs()
    if not skill_dirs:
        print("No skill directories found under skills/ — nothing to validate.")
        return 0

    structure_errors = check_structure(skill_dirs)
    insularity_errors = check_insularity(skill_dirs)
    all_errors = structure_errors + insularity_errors

    if not all_errors:
        print(f"OK: {len(skill_dirs)} skills passed structure and insularity checks.")
        return 0

    print(f"FAIL: {len(all_errors)} issue(s) across {len(skill_dirs)} skills:\n")
    for error in structure_errors:
        print(f"  [structure]  {error}")
    for error in insularity_errors:
        print(f"  [insularity] {error}")
    return 1


if __name__ == "__main__":
    sys.exit(main())
