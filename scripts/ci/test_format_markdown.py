#!/usr/bin/env python3
"""Regression tests for the Markdown formatter."""

from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

import format_markdown


class FormatMarkdownTests(unittest.TestCase):
    def test_preserves_yaml_front_matter(self) -> None:
        original = """---
triggers:
  - "example"
category: workflow
priority: core
---

# Heading

Text
"""

        with tempfile.TemporaryDirectory() as temporary_directory:
            path = Path(temporary_directory) / "skill.md"
            path.write_text(original, encoding="utf-8")

            format_markdown.format_file(path, write_back=True)
            formatted = path.read_text(encoding="utf-8")
            expected_front_matter = original.split("\n\n# Heading", 1)[0] + "\n"

            self.assertTrue(formatted.startswith(expected_front_matter))
            self.assertFalse(format_markdown.format_file(path, write_back=False))


if __name__ == "__main__":
    unittest.main()
