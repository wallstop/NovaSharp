#!/usr/bin/env python3
"""Regression tests for the GitHub Pages Liquid syntax guard."""

from __future__ import annotations

import unittest
from pathlib import Path

import check_jekyll_liquid


# (label, markdown, expected number of findings)
SCAN_CASES = (
    # The exact construct that broke every Pages build from 2026-07-01 onward.
    ("lua_nested_table_constructor", "local t = {{n=2}, {n=1}}\n", 1),
    ("spaced_nested_table_constructor", "local t = { {n=2}, {n=1} }\n", 0),
    ("well_formed_variable", "Title: {{ site.title }}\n", 0),
    ("well_formed_variable_with_filter", "{{ page.title | upcase }}\n", 0),
    ("variable_closed_by_single_brace", "{{ oops }\n", 1),
    ("variable_never_closed", "trailing {{ oops\n", 1),
    ("two_defects_on_one_line", "{{a} and {{b}\n", 2),
    ("known_tag", "{% if page.title %}x{% endif %}\n", 0),
    ("unknown_tag", "{% nope %}\n", 1),
    ("empty_tag", "{%  %}\n", 1),
    ("unterminated_tag", "{% if page.title\n", 1),
    ("raw_suppresses_variable", "{% raw %}{{n=2}, {n=1}}{% endraw %}\n", 0),
    ("raw_opened_with_whitespace_control", "{%- raw -%}{{n=2}{% endraw %}\n", 0),
    # Liquid::Raw#parse has no whitespace-control branch, so `{%- endraw -%}`
    # leaves the raw region open and the build fails.
    ("raw_closed_with_whitespace_control", "{%- raw -%}{{n=2}{%- endraw -%}\n", 1),
    ("endraw_lookalike_does_not_close", "{% raw %}{{n=2}{% endrawx %}\n", 1),
    ("unclosed_raw", "{% raw %}{{n=2}\n", 1),
    ("no_liquid_at_all", "# Heading\n\nJust prose and `code`.\n", 0),
    ("closing_braces_only", "end of table }}\n", 0),
)


# (relative path, rendered?) evaluated against the repository's real `_config.yml`.
RENDERED_CASES = (
    ("docs/guide.md", True),
    ("docs/guide.markdown", True),
    ("README.md", True),
    (".llm/skills/example.md", False),
    ("docs/_drafts/example.md", False),
    ("_layouts/default.md", False),
    ("docs/notes.txt", False),
    ("src/Runtime.cs", False),
    # Excluded by `_config.yml`, so free to quote Liquid delimiters.
    ("PLAN.md", False),
    ("progress/session-001-example.md", False),
    ("scripts/ci/README.md", False),
    ("tools/LuaCorpusExtractor/README.md", False),
)


class ScanTextTests(unittest.TestCase):
    def test_findings_match_expected_counts(self) -> None:
        for label, markdown, expected in SCAN_CASES:
            with self.subTest(label=label):
                findings = check_jekyll_liquid.scan_text(markdown, "sample.md")

                self.assertEqual(
                    expected,
                    len(findings),
                    f"{label}: {[finding.message for finding in findings]}",
                )

    def test_reports_line_and_column_of_the_offending_delimiter(self) -> None:
        markdown = "# Heading\n\n```lua\nlocal t = {{n=2}, {n=1}}\n```\n"

        (finding,) = check_jekyll_liquid.scan_text(markdown, "docs/spec.md")

        self.assertEqual("docs/spec.md", finding.path)
        self.assertEqual(4, finding.line)
        self.assertEqual(11, finding.column)
        self.assertIn("{{n=2}", finding.snippet)

    def test_scans_past_a_closed_raw_region(self) -> None:
        markdown = "{% raw %}{{safe}{% endraw %}\nlocal t = {{n=2}\n"

        (finding,) = check_jekyll_liquid.scan_text(markdown, "sample.md")

        self.assertEqual(2, finding.line)


class IsRenderedTests(unittest.TestCase):
    def test_matches_jekyll_entry_filtering_and_config_excludes(self) -> None:
        excludes = check_jekyll_liquid.load_excludes()

        for relative_path, expected in RENDERED_CASES:
            with self.subTest(relative_path=relative_path):
                self.assertEqual(
                    expected,
                    check_jekyll_liquid.is_rendered(Path(relative_path), excludes),
                )

    def test_config_excludes_are_loaded_without_slashes(self) -> None:
        excludes = check_jekyll_liquid.load_excludes()

        self.assertIn("progress", excludes)
        self.assertIn("PLAN.md", excludes)
        self.assertFalse([entry for entry in excludes if entry.endswith("/")])

    def test_unreadable_config_scans_everything(self) -> None:
        """Publishing everything is the state that broke the site; never narrow."""
        self.assertEqual(
            (), check_jekyll_liquid.load_excludes(Path("does/not/exist/_config.yml"))
        )
        self.assertTrue(check_jekyll_liquid.is_rendered(Path("PLAN.md"), ()))

    def test_exclude_matches_directories_not_name_prefixes(self) -> None:
        self.assertTrue(
            check_jekyll_liquid.is_rendered(Path("docsite/guide.md"), ("docs",))
        )
        self.assertFalse(
            check_jekyll_liquid.is_rendered(Path("docs/guide.md"), ("docs",))
        )


class RepositoryTests(unittest.TestCase):
    def test_repository_markdown_is_free_of_fatal_liquid(self) -> None:
        """The published site must build; this is the guard's real contract."""
        candidates = check_jekyll_liquid.discover_files(check_jekyll_liquid.REPO_ROOT)

        self.assertGreater(len(candidates), 0)
        self.assertEqual(
            [],
            [
                finding.format()
                for finding in check_jekyll_liquid.check_files(
                    candidates, check_jekyll_liquid.REPO_ROOT
                )
            ],
        )


if __name__ == "__main__":
    unittest.main()
