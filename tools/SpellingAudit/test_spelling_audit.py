#!/usr/bin/env python3
"""Focused tests for the spelling-audit wrapper."""

from __future__ import annotations

import subprocess
import unittest
from unittest import mock

import spelling_audit


class DiscoverDefaultPathsTests(unittest.TestCase):
    """Verify that default targets match the content being committed."""

    @mock.patch.object(spelling_audit.subprocess, "run")
    def test_uses_staged_index_and_collapses_paths_to_roots(
        self, run: mock.Mock
    ) -> None:
        run.return_value = subprocess.CompletedProcess(
            args=[],
            returncode=0,
            stdout=(
                ".agents/skills\0.claude/skills\0.llm/context.md\0"
                ".llm/skills/adding-skills/SKILL.md\0README.md\0"
                "artifacts/output.txt\0"
            ),
        )

        paths = spelling_audit.discover_default_paths()

        self.assertEqual((".agents", ".claude", ".llm", "README.md"), paths)
        run.assert_called_once_with(
            ["git", "ls-files", "--cached", "-z"],
            capture_output=True,
            text=True,
            cwd=spelling_audit.ROOT,
            check=True,
        )

    @mock.patch.object(spelling_audit.subprocess, "run")
    def test_case_colliding_roots_have_total_order(self, run: mock.Mock) -> None:
        run.return_value = subprocess.CompletedProcess(
            args=[], returncode=0, stdout="docs/a.md\0Docs/b.md\0"
        )

        self.assertEqual(("Docs", "docs"), spelling_audit.discover_default_paths())

    @mock.patch.object(spelling_audit.subprocess, "run")
    def test_fails_when_git_index_is_unavailable(self, run: mock.Mock) -> None:
        run.side_effect = FileNotFoundError("git")

        with self.assertRaisesRegex(
            spelling_audit.SpellingAuditError, "staged Git index"
        ):
            spelling_audit.discover_default_paths()


if __name__ == "__main__":
    unittest.main()
