#!/usr/bin/env python3
"""Enforce PLAN.md as a lean queue for active and future work."""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

MAX_LINES = 120
BLOCK_QUOTE_PREFIX = r"(?:[ \t]{0,3}>[ \t]?)*[ \t]*"
LIST_MARKER_PREFIX = r"(?:(?:[-+*]|\d{1,9}[.)])[ \t]+)?"
COMPLETED_CHECKBOX = re.compile(
    rf"^{BLOCK_QUOTE_PREFIX}(?:[-+*]|\d{{1,9}}[.)])[ \t]+\[[xX]\]",
    re.MULTILINE,
)
SESSION_LINK = "progress/session-"
MARKDOWN_HEADING = re.compile(
    rf"^{BLOCK_QUOTE_PREFIX}#{{1,6}}[ \t]+(?P<title>[^\n]+)$",
    re.IGNORECASE | re.MULTILINE,
)
ARCHIVE_TITLE = re.compile(
    rf"(?:completed\b[^\n]*"
    rf"|(?:history|archive|retrospective|progress log|session log|past (?:results?|work)|results? archive|completion summary|validation receipt)[ \t]*"
    rf"|historical(?:[ \t]+repository)?[ \t]+snapshot[ \t]*"
    rf"|done(?:[ \t]+(?:work|items|tasks|initiatives|milestones))?[ \t]*(?:✅[ \t]*)?"
    rf"|finished[ \t]+(?:work|items|tasks|initiatives|milestones)[ \t]*"
    rf"|closed[ \t]+(?:work|items|tasks|initiatives|milestones|issues)[ \t]*"
    rf"|previous[ \t]+work[ \t]*"
    rf"|(?:progress|results?)[ \t]*"
    rf"|.*✅[^\n]*\b(?:complete(?:d)?|resolved|fixed|incorporated)\b"
    rf"|complete(?:d)?[ \t]*)",
    re.IGNORECASE | re.MULTILINE,
)
DATE_LED_LINE = re.compile(
    rf"^{BLOCK_QUOTE_PREFIX}{LIST_MARKER_PREFIX}\d{{4}}-\d{{2}}-\d{{2}}\b"
    rf"[ \t]*(?::|—|–|-)?[ \t]*(?P<body>[^\n]*)$",
    re.IGNORECASE | re.MULTILINE,
)
EMPHASIZED_RESULT = re.compile(
    rf"^{BLOCK_QUOTE_PREFIX}{LIST_MARKER_PREFIX}(?:\*\*|__)(?:completed|done|passed|failed|verified|green|results?)(?:\*\*|__)[ \t]*:[^\n]*\b\d{{4}}-\d{{2}}-\d{{2}}\b",
    re.IGNORECASE | re.MULTILINE,
)
PROGRESS_NARRATIVE = re.compile(
    rf"^{BLOCK_QUOTE_PREFIX}{LIST_MARKER_PREFIX}(?:(?:\*\*|__)(?:progress|(?:current[ \t]+)?status)(?:\*\*|__)"
    rf"|(?:progress|(?:current[ \t]+)?status))[ \t]*:",
    re.IGNORECASE | re.MULTILINE,
)
COMPLETION_LINE = re.compile(
    rf"^{BLOCK_QUOTE_PREFIX}{LIST_MARKER_PREFIX}✅(?=[ \t]|$)",
    re.IGNORECASE | re.MULTILINE,
)
UNEMPHASIZED_RESULT = re.compile(
    rf"^{BLOCK_QUOTE_PREFIX}{LIST_MARKER_PREFIX}(?:completed|done|results?)[ \t]*(?::|—|–|-)",
    re.IGNORECASE | re.MULTILINE,
)


def normalize_heading_title(title: str) -> str:
    """Remove matching outer Markdown emphasis markers from a heading title."""
    normalized = title.strip().replace("**", "").replace("__", "")
    while len(normalized) >= 2:
        if (normalized.startswith("**") and normalized.endswith("**")) or (
            normalized.startswith("__") and normalized.endswith("__")
        ):
            normalized = normalized[2:-2].strip()
            continue
        if (normalized.startswith("*") and normalized.endswith("*")) or (
            normalized.startswith("_") and normalized.endswith("_")
        ):
            normalized = normalized[1:-1].strip()
            continue
        break
    return normalized


def find_violations(content: str, max_lines: int = MAX_LINES) -> list[str]:
    """Return actionable PLAN hygiene violations."""
    violations: list[str] = []
    line_count = len(content.splitlines())
    if line_count > max_lines:
        violations.append(f"has {line_count} lines; maximum is {max_lines}")

    completed_count = len(COMPLETED_CHECKBOX.findall(content))
    if completed_count:
        violations.append(f"contains {completed_count} completed checklist item(s)")

    session_link_count = content.count(SESSION_LINK)
    if session_link_count:
        violations.append(f"contains {session_link_count} session-history link(s)")

    archive_headings = []
    for heading in MARKDOWN_HEADING.finditer(content):
        normalized_title = normalize_heading_title(heading.group("title"))
        if ARCHIVE_TITLE.fullmatch(normalized_title):
            archive_headings.append(heading)
    if archive_headings:
        violations.append(f"contains {len(archive_headings)} archive-style heading(s)")

    date_led_lines = list(DATE_LED_LINE.finditer(content))
    if date_led_lines:
        violations.append(f"contains {len(date_led_lines)} date-led line(s)")

    emphasized_results = list(EMPHASIZED_RESULT.finditer(content))
    if emphasized_results:
        violations.append(
            f"contains {len(emphasized_results)} dated completion result line(s)"
        )

    progress_narratives = list(PROGRESS_NARRATIVE.finditer(content))
    if progress_narratives:
        violations.append(f"contains {len(progress_narratives)} progress narrative(s)")

    completion_lines = list(COMPLETION_LINE.finditer(content))
    completion_lines.extend(UNEMPHASIZED_RESULT.finditer(content))
    if completion_lines:
        violations.append(f"contains {len(completion_lines)} completion status line(s)")

    return violations


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("path", nargs="?", type=Path, default=Path("PLAN.md"))
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    try:
        content = args.path.read_text(encoding="utf-8")
    except (OSError, UnicodeDecodeError) as exc:
        print(f"PLAN hygiene check could not read {args.path}: {exc}", file=sys.stderr)
        return 1

    violations = find_violations(content)
    if not violations:
        print(f"PLAN hygiene check passed: {args.path}")
        return 0

    print(f"PLAN hygiene check failed: {args.path}", file=sys.stderr)
    for violation in violations:
        print(f"- {violation}", file=sys.stderr)
    print(
        "Move history to progress/, durable context to .llm or docs, and backlog detail to issues.",
        file=sys.stderr,
    )
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
