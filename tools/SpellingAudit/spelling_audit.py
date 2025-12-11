#!/usr/bin/env python3
"""Spelling audit helper for NovaSharp.

Wraps the `codespell` CLI so we can produce deterministic logs and wire the
results into CI. The script mirrors the naming/namespace audit helpers: run it
with `--write-log spelling_audit.log` to refresh the committed report, or use
`--verify-log spelling_audit.log` to fail when the log is stale.
"""

from __future__ import annotations

import argparse
import difflib
import subprocess
import sys
from pathlib import Path
from typing import Iterable, Sequence

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_LOG = ROOT / "spelling_audit.log"
DEFAULT_ALLOWLIST = ROOT / "tools" / "SpellingAudit" / "allowlist.txt"
TOP_LEVEL_EXCLUDES = {".git", ".vs", "artifacts"}


def discover_default_paths() -> tuple[str, ...]:
    """Discover scan targets from git-tracked files for deterministic output.

    Uses `git ls-tree` to get a consistent list of root-level entries that are
    tracked in version control. This ensures the scan targets are identical
    across different environments (local dev, CI) regardless of untracked files.

    Falls back to filesystem discovery if git is unavailable.
    """
    try:
        proc = subprocess.run(
            ["git", "ls-tree", "--name-only", "HEAD"],
            capture_output=True,
            text=True,
            cwd=ROOT,
            check=True,
        )
        entries = sorted(
            (line.strip() for line in proc.stdout.splitlines() if line.strip()),
            key=str.lower,
        )
        return tuple(e for e in entries if e not in TOP_LEVEL_EXCLUDES)
    except (subprocess.CalledProcessError, FileNotFoundError):
        # Fallback: scan filesystem if git is unavailable
        entries = []
        for child in sorted(ROOT.iterdir(), key=lambda p: p.name.lower()):
            if child.name in TOP_LEVEL_EXCLUDES:
                continue
            entries.append(child.name)
        return tuple(entries)


DEFAULT_PATHS = discover_default_paths()
DEFAULT_SKIP_GLOBS: tuple[str, ...] = (
    ".git",
    ".git/*",
    ".git\\*",
    ".vs",
    ".vs/*",
    ".vs\\*",
    "src/.vs",
    "src/.vs/*",
    "src\\.vs\\*",
    "artifacts",
    "artifacts/*",
    "artifacts\\*",
    "docs/coverage",
    "docs/coverage/*",
    "docs\\coverage",
    "docs\\coverage\\*",
    "build",
    "coverage-html.tgz",
    "*.dll",
    "*.exe",
    "*.bin",
    "*.pdb",
    "*.obj",
    "*.lock",
    "*.log",
    "*.vsidx",
    "*.png",
    "*.jpg",
    "*.jpeg",
    "*.gif",
    "*.bmp",
    "*.ico",
    "*.svg",
    "*.ttf",
    "*.woff",
    "*.woff2",
    "*.eot",
    "*.pdf",
    "*.zip",
    "*.tar",
    "*.tar.gz",
    "*.tgz",
    "*.gz",
    "*.7z",
    "*.nupkg",
    "*.luac",
    "*.snap",
    "src/debuggers/WallstopStudios.NovaSharp.RemoteDebugger/Resources/theme.css",
    "src\\debuggers\\WallstopStudios.NovaSharp.RemoteDebugger\\Resources\\theme.css",
    "*/bin/*",
    "*\\bin\\*",
    "*/obj/*",
    "*\\obj\\*",
    "*/packages/*",
    "*\\packages\\*",
)


class SpellingAuditError(RuntimeError):
    """Raised when the spelling audit cannot complete."""


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="NovaSharp spelling audit helper")
    parser.add_argument(
        "paths",
        nargs="*",
        default=DEFAULT_PATHS,
        help="Paths (relative to the repo root) to scan. Defaults to the entire repository.",
    )
    parser.add_argument(
        "--write-log",
        nargs="?",
        type=Path,
        const=DEFAULT_LOG,
        help="Write the audit report to the specified path (defaults to spelling_audit.log).",
    )
    parser.add_argument(
        "--verify-log",
        nargs="?",
        type=Path,
        const=DEFAULT_LOG,
        help="Verify that the current audit matches the given log (defaults to spelling_audit.log).",
    )
    parser.add_argument(
        "--allowlist",
        type=Path,
        default=DEFAULT_ALLOWLIST,
        help="Path to the word allowlist (one entry per line; # comments are supported).",
    )
    parser.add_argument(
        "--skip",
        action="append",
        default=[],
        help="Additional glob patterns to skip (comma-separated or repeated flags).",
    )
    parser.add_argument(
        "--quiet-level",
        type=int,
        default=7,
        help="codespell quiet level (default: 7 to suppress binary/encoding noise).",
    )
    return parser.parse_args()


def flatten_patterns(raw_patterns: Iterable[str]) -> list[str]:
    globs: list[str] = []
    for raw in raw_patterns:
        if not raw:
            continue
        parts = [chunk.strip() for chunk in raw.split(",") if chunk.strip()]
        globs.extend(parts)
    # Preserve order while discarding duplicates for deterministic output.
    seen: set[str] = set()
    ordered: list[str] = []
    for item in list(DEFAULT_SKIP_GLOBS) + globs:
        if item in seen:
            continue
        seen.add(item)
        ordered.append(item)
    return ordered


def load_allowlist(path: Path) -> list[str]:
    if not path.exists():
        return []
    words: list[str] = []
    for line in path.read_text(encoding="utf-8").splitlines():
        entry = line.split("#", 1)[0].strip()
        if entry:
            words.append(entry)
    return words


def run_codespell(paths: Sequence[str], allowlist: list[str], skip_globs: Sequence[str], quiet: int) -> list[str]:
    try:
        __import__("codespell_lib")
    except ModuleNotFoundError as exc:  # pragma: no cover - defensive guard
        raise SpellingAuditError(
            "codespell_lib is not installed. Run 'python -m pip install -r requirements.tooling.txt' first."
        ) from exc

    command = [sys.executable, "-m", "codespell_lib", "-q", str(quiet)]
    if skip_globs:
        command.extend(["-S", ",".join(skip_globs)])
    if allowlist:
        command.extend(["-L", ",".join(allowlist)])
    command.extend(str(Path(p)) for p in paths)

    proc = subprocess.run(
        command,
        capture_output=True,
        text=True,
        cwd=ROOT,
        check=False,
    )

    if proc.returncode not in (0, 65):
        stderr = proc.stderr.strip()
        raise SpellingAuditError(stderr or f"codespell returned exit code {proc.returncode}")

    output = [line.strip() for line in proc.stdout.splitlines() if line.strip()]
    return output


def render_report(
    findings: Sequence[str],
    paths: Sequence[str],
    skip_globs: Sequence[str],
    allowlist: Sequence[str],
) -> str:
    lines: list[str] = [
        "# Spelling Audit Report",
        "",
        "_Generated via tools/SpellingAudit/spelling_audit.py_",
        "",
    ]
    lines.append(f"Scan targets: {', '.join(paths)}")
    lines.append(f"Skip globs: {', '.join(skip_globs) if skip_globs else '<none>'}")
    lines.append(
        f"Allowlisted words: {', '.join(allowlist) if allowlist else '<none>'}"
    )
    lines.append("")

    if findings:
        lines.append(f"Found {len(findings)} potential spelling issue(s).")
        lines.append("")
        lines.append("## Outstanding Findings")
        lines.append("")
        lines.extend(f"- {entry}" for entry in findings)
    else:
        lines.append("All inspected files passed the spelling audit.")
    lines.append("")
    return "\n".join(lines)


def write_log(path: Path, contents: str) -> None:
    path.write_text(contents, encoding="utf-8", newline="\n")


def verify_log(path: Path, expected: str) -> None:
    if not path.exists():
        raise SpellingAuditError(
            f"{path} does not exist. Run the audit with --write-log {path} before verifying."
        )
    current = path.read_text(encoding="utf-8")
    if current == expected:
        return
    diff = "\n".join(
        difflib.unified_diff(
            current.splitlines(),
            expected.splitlines(),
            fromfile=str(path),
            tofile="<generated>",
            lineterm="",
        )
    )
    raise SpellingAuditError(
        f"Spelling audit log is out of date. Run 'python tools/SpellingAudit/spelling_audit.py --write-log {path}'\n{diff}"
    )


def main() -> None:
    args = parse_args()
    if args.write_log and args.verify_log:
        raise SpellingAuditError("Use either --write-log or --verify-log, not both.")

    skip_globs = flatten_patterns(args.skip)
    allowlist = load_allowlist(args.allowlist)
    findings = run_codespell(args.paths, allowlist, skip_globs, args.quiet_level)
    report = render_report(findings, args.paths, skip_globs, allowlist)

    if args.write_log:
        write_log(args.write_log.resolve(), report)
        return
    if args.verify_log:
        verify_log(args.verify_log.resolve(), report)
        return

    # Default behaviour: print the report for convenience when running interactively.
    sys.stdout.write(report)
    if findings:
        raise SpellingAuditError(
            "Spelling audit found one or more issues. Update the affected files or refresh spelling_audit.log."
        )


if __name__ == "__main__":
    try:
        main()
    except SpellingAuditError as exc:
        sys.stderr.write(f"[spelling-audit] {exc}\n")
        sys.exit(1)
