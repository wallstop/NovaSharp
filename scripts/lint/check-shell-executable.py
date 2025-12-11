#!/usr/bin/env python3
"""
Fails when shell scripts (.sh files) are not marked as executable in git.

This check prevents CI failures from permission denied errors when running
shell scripts on Linux/macOS runners.

The fix is simple: git update-index --chmod=+x <script.sh>
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[2]


def get_non_executable_scripts() -> list[tuple[str, str]]:
    """Returns list of (mode, path) for shell scripts without executable bit."""
    try:
        result = subprocess.run(
            ["git", "ls-files", "-s", "*.sh"],
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

    violations = []
    for line in result.stdout.strip().splitlines():
        if not line:
            continue
        # Format: mode sha1 stage\tpath
        parts = line.split()
        if len(parts) < 4:
            continue
        mode = parts[0]
        path = parts[3]
        # 100755 = executable, 100644 = not executable
        if mode == "100644":
            violations.append((mode, path))

    return violations


def main() -> int:
    violations = get_non_executable_scripts()

    if violations:
        print(
            "Shell scripts must have executable permission in git.\n"
            "The following scripts are missing the executable bit:\n",
            file=sys.stderr,
        )
        for mode, path in violations:
            print(f"  {path} (mode: {mode})", file=sys.stderr)

        print(
            "\nTo fix, run:\n"
            "  git update-index --chmod=+x <script.sh>\n"
            "\nOr fix all at once:\n"
            "  git ls-files -s '*.sh' | grep '^100644' | "
            "awk '{print $4}' | xargs git update-index --chmod=+x",
            file=sys.stderr,
        )
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
