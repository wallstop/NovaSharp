#!/usr/bin/env python3
"""
Checks that shell scripts invoke Python scripts using explicit python interpreter.

Direct execution of Python scripts (./script.py or script.py) requires executable
permissions in git, which is fragile across platforms and CI environments.

Instead, shell scripts should use: python "path/to/script.py"
"""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]

# Pattern to detect direct Python script execution without python prefix
# Matches lines like:
#   scripts/lint/foo.py
#   "${REPO_ROOT}/scripts/lint/foo.py"
#   ./scripts/lint/foo.py
# But NOT:
#   python scripts/lint/foo.py
#   python "${REPO_ROOT}/scripts/lint/foo.py"
DIRECT_PYTHON_PATTERN = re.compile(
    r'^(?!.*\bpython\b)'  # Negative lookahead: line does NOT contain 'python'
    r'.*'                 # Any characters
    r'["\']?'             # Optional opening quote
    r'(?:\$\{[^}]+\})?'   # Optional variable like ${REPO_ROOT}
    r'[/\\]?'             # Optional leading slash
    r'[\w./\\-]+'         # Path characters
    r'\.py'               # .py extension
    r'["\']?'             # Optional closing quote
    r'\s*$',              # End of line (with optional whitespace)
    re.MULTILINE,
)


def get_shell_scripts() -> list[Path]:
    """Returns list of shell script paths in the repository."""
    try:
        result = subprocess.run(
            ["git", "ls-files", "*.sh"],
            cwd=REPO_ROOT,
            check=True,
            capture_output=True,
            text=True,
        )
    except subprocess.CalledProcessError as exc:
        print(f"git ls-files failed: {exc.stderr}", file=sys.stderr)
        raise SystemExit(1) from exc
    except FileNotFoundError as exc:
        raise SystemExit("git is required to run this check.") from exc

    return [REPO_ROOT / p.strip() for p in result.stdout.strip().splitlines() if p.strip()]


def check_script(path: Path) -> list[tuple[int, str]]:
    """Returns list of (line_number, line) for violations in the given script."""
    try:
        content = path.read_text(encoding="utf-8")
    except (OSError, UnicodeDecodeError) as exc:
        print(f"Warning: Could not read {path}: {exc}", file=sys.stderr)
        return []

    violations = []
    for i, line in enumerate(content.splitlines(), start=1):
        stripped = line.strip()
        # Skip comments and empty lines
        if not stripped or stripped.startswith("#"):
            continue
        # Check if this line directly executes a .py file without python
        if stripped.endswith(".py") or stripped.endswith('.py"') or stripped.endswith(".py'"):
            # Make sure it's not prefixed with python
            if "python" not in stripped.lower():
                violations.append((i, line))

    return violations


def main() -> int:
    violations = []
    for script_path in get_shell_scripts():
        script_violations = check_script(script_path)
        for line_num, line in script_violations:
            rel_path = script_path.relative_to(REPO_ROOT)
            violations.append(f"{rel_path}:{line_num}: {line.strip()}")

    if violations:
        print(
            "Shell scripts must invoke Python scripts using explicit 'python' interpreter.\n"
            "Direct execution requires executable permissions which is fragile in CI.\n\n"
            "The following lines directly execute .py files without 'python':\n",
            file=sys.stderr,
        )
        for entry in violations:
            print(f"  {entry}", file=sys.stderr)
        print(
            "\nTo fix, change lines like:\n"
            '  "${REPO_ROOT}/scripts/lint/check-foo.py"\n'
            "To:\n"
            '  python "${REPO_ROOT}/scripts/lint/check-foo.py"\n',
            file=sys.stderr,
        )
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
