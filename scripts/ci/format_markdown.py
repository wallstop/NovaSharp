#!/usr/bin/env python3
"""Format Markdown files consistently using mdformat."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path
from typing import Iterable, List, Sequence

try:
    import mdformat
except ModuleNotFoundError as exc:  # pragma: no cover - guidance for missing deps
    raise SystemExit(
        "mdformat is not installed. Run `python -m pip install -r requirements.tooling.txt`."
    ) from exc


REPO_ROOT = Path(__file__).resolve().parents[2]
EXCLUDE_DIRS = (
    "artifacts",
    "docs/coverage",
    "node_modules",
    ".git",
)
SKIP_FILES = {
    "AGENTS.md",
    "CLAUDE.md",
    "PLAN.md",
    "README.md",
    "docs/modernization/moonsharp-issue-audit.md",
    "docs/testing/spec-audit.md",
}
MD_EXTENSIONS = {"gfm"}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    mode_group = parser.add_mutually_exclusive_group(required=True)
    mode_group.add_argument("--check", action="store_true", help="Fail if formatting differs.")
    mode_group.add_argument("--fix", action="store_true", help="Rewrite files in-place.")

    scope_group = parser.add_mutually_exclusive_group(required=True)
    scope_group.add_argument(
        "--files",
        nargs="+",
        help="Specific Markdown files or folders to process.",
    )
    scope_group.add_argument(
        "--all",
        action="store_true",
        help="Process every Markdown file under the repository.",
    )
    parser.add_argument(
        "--include-skipped",
        action="store_true",
        help="Also process files normally excluded (AGENTS.md, CLAUDE.md, PLAN.md, README.md).",
    )
    return parser.parse_args()


def should_skip(path: Path, include_skipped: bool) -> bool:
    try:
        rel = path.relative_to(REPO_ROOT)
    except ValueError:
        rel_str = path.as_posix()
    else:
        rel_str = rel.as_posix()

    if any(rel_str == item or rel_str.startswith(f"{item}/") for item in EXCLUDE_DIRS):
        return True

    if not include_skipped and rel_str in SKIP_FILES:
        return True

    return False


def iter_markdown_files(targets: Sequence[str] | None, include_skipped: bool) -> List[Path]:
    paths: List[Path] = []

    if not targets:
        roots = [REPO_ROOT]
    else:
        roots = []
        for target in targets:
            target_path = Path(target)
            if not target_path.is_absolute():
                target_path = REPO_ROOT / target_path
            roots.append(target_path.resolve())

    for root in roots:
        if not root.exists():
            continue

        if root.is_dir():
            for candidate in sorted(root.rglob("*.md")):
                if not should_skip(candidate, include_skipped):
                    paths.append(candidate)
        else:
            if root.suffix.lower() == ".md" and not should_skip(root, include_skipped):
                paths.append(root)

    # Deduplicate while preserving order
    seen = set()
    unique_paths = []
    for path in paths:
        rel = path.relative_to(REPO_ROOT)
        if rel not in seen:
            unique_paths.append(path)
            seen.add(rel)

    return unique_paths


def format_file(path: Path, write_back: bool) -> bool:
    original = path.read_text(encoding="utf-8")
    formatted = mdformat.text(original, extensions=MD_EXTENSIONS)
    if original == formatted:
        return False

    if write_back:
        path.write_text(formatted, encoding="utf-8", newline="\n")

    return True


def run(files: Iterable[Path], fix: bool) -> List[Path]:
    changed: List[Path] = []
    for file_path in files:
        if format_file(file_path, write_back=fix):
            changed.append(file_path)
    return changed


def main() -> int:
    args = parse_args()
    targets = None if args.all else args.files
    files = iter_markdown_files(targets, include_skipped=args.include_skipped)

    if not files:
        print("No Markdown files to process.")
        return 0

    changed = run(files, fix=args.fix)

    if args.check and changed:
        rel_list = ", ".join(str(path.relative_to(REPO_ROOT)) for path in changed)
        print("Markdown formatting issues detected in:", rel_list)
        print("Run `python scripts/ci/format_markdown.py --fix --files <paths>` to update them.")
        return 1

    if args.fix:
        if changed:
            print(f"Formatted {len(changed)} Markdown file(s).")
        else:
            print("Markdown files already formatted.")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
