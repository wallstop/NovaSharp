#!/usr/bin/env python3
"""Regression tests for the GitHub Pages Liquid syntax guard."""

from __future__ import annotations

import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

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
    # Jekyll's `markdown_ext` default is five extensions, not just `.md`.
    ("docs/guide.mkd", True),
    ("docs/guide.mkdn", True),
    ("docs/guide.mkdown", True),
    # Excluded by `_config.yml`, so free to quote Liquid delimiters.
    ("PLAN.md", False),
    ("progress/session-001-example.md", False),
    ("scripts/ci/README.md", False),
    ("tools/LuaCorpusExtractor/README.md", False),
)


# (label, markdown, does Jekyll abort the build?)
#
# Every row below was verified by building it under a real `github-pages` v232
# install, one build per row, and comparing Jekyll's exit code against this
# guard's verdict. 52/52 agree. Where a row looks surprising, Jekyll's behaviour
# is the reason — `Liquid::Comment#unknown_tag` is a no-op, so a comment hides a
# stray end tag but still parses variables; `break` and `continue` are registered
# globally, so they are legal outside a loop.
FATALITY_CASES = (
    # Variable delimiters
    ("lua_nested_table", "local t = {{n=2}, {n=1}}\n", True),
    ("spaced_nested_table", "local t = { {n=2}, {n=1} }\n", False),
    ("well_formed_variable", "Title: {{ site.title }}\n", False),
    ("variable_filter", "{{ page.title | upcase }}\n", False),
    ("var_single_brace", "{{ oops }\n", True),
    ("var_never_closed", "trailing {{ oops\n", True),
    ("two_defects_one_line", "{{a} and {{b}\n", True),
    ("closing_braces_only", "end of table }}\n", False),
    ("no_liquid", "# H\n\nprose and `code`.\n", False),
    # Tag delimiters and unknown names
    ("unknown_tag", "{% nope %}\n", True),
    ("empty_tag", "{%  %}\n", True),
    ("unterminated_tag", "{% if page.title\n", True),
    ("echo_tag", "{% echo x %}\n", True),
    ("assign_standalone", "{% assign x = 1 %}\n", False),
    # Raw regions
    ("raw_suppresses_var", "{% raw %}{{n=2}, {n=1}}{% endraw %}\n", False),
    ("raw_open_ws_control", "{%- raw -%}{{n=2}{% endraw %}\n", False),
    ("raw_close_ws_control", "{%- raw -%}{{n=2}{%- endraw -%}\n", True),
    ("endraw_lookalike", "{% raw %}{{n=2}{% endrawx %}\n", True),
    ("unclosed_raw", "{% raw %}{{n=2}\n", True),
    ("raw_inside_if", "{% if x %}{% raw %}{{n=2}{% endraw %}{% endif %}\n", False),
    # Orphan block-only tags — the class a flat allowlist misses
    ("orphan_endfor", "{% endfor %}\n", True),
    ("orphan_endif", "{% endif %}\n", True),
    ("orphan_else", "{% else %}\n", True),
    ("orphan_elsif", "{% elsif x %}\n", True),
    ("orphan_when", "{% when 1 %}\n", True),
    ("orphan_endcapture", "{% endcapture %}\n", True),
    ("orphan_endraw", "{% endraw %}\n", True),
    ("orphan_break", "{% break %}\n", False),
    ("orphan_continue", "{% continue %}\n", False),
    # Unclosed blocks
    ("unclosed_if", "{% if x %}text\n", True),
    ("unclosed_for", "{% for a in b %}text\n", True),
    ("unclosed_capture", "{% capture v %}x\n", True),
    ("unclosed_comment", "{% comment %}x\n", True),
    ("unclosed_highlight", "{% highlight ruby %}code\n", True),
    # Mismatched and crossed delimiters
    ("mismatched_end", "{% if x %}text{% endfor %}\n", True),
    ("crossed_nesting", "{% for a in b %}{% if a %}x{% endfor %}{% endif %}\n", True),
    # Well-formed blocks
    ("proper_if", "{% if x %}text{% endif %}\n", False),
    ("proper_for_else", "{% for a in b %}x{% else %}y{% endfor %}\n", False),
    ("proper_case", "{% case x %}{% when 1 %}a{% endcase %}\n", False),
    ("proper_capture", "{% capture v %}x{% endcapture %}\n", False),
    ("proper_highlight", "{% highlight ruby %}c{% endhighlight %}\n", False),
    ("proper_comment", "{% comment %}x{% endcomment %}\n", False),
    ("proper_ifchanged", "{% ifchanged %}x{% endifchanged %}\n", False),
    ("proper_tablerow", "{% tablerow a in b %}x{% endtablerow %}\n", False),
    ("break_inside_for", "{% for a in b %}{% break %}{% endfor %}\n", False),
    ("nested_if_in_for", "{% for a in b %}{% if a %}x{% endif %}{% endfor %}\n", False),
    # Inner-tag placement
    ("when_inside_if", "{% if x %}{% when 1 %}{% endif %}\n", True),
    ("else_inside_case", "{% case x %}{% else %}a{% endcase %}\n", False),
    ("elsif_inside_unless", "{% unless x %}a{% elsif y %}b{% endunless %}\n", False),
    # Comments discard names Liquid cannot resolve, but nothing else. Registered
    # block openers are still parsed inside a comment body and still need closers,
    # and a nested block restores normal unknown-tag behaviour.
    ("comment_hides_end", "{% comment %}{% endfor %}{% endcomment %}\n", False),
    ("comment_hides_unknown", "{% comment %}{% nope %}{% endcomment %}\n", False),
    ("comment_hides_orphan_else", "{% comment %}{% else %}{% endcomment %}\n", False),
    ("comment_hides_orphan_when", "{% comment %}{% when 1 %}{% endcomment %}\n", False),
    ("comment_bad_variable", "{% comment %}{{n=2}{% endcomment %}\n", True),
    ("comment_unclosed_if", "{% comment %}{% if x %}{% endcomment %}\n", True),
    ("comment_closed_if", "{% comment %}{% if x %}y{% endif %}{% endcomment %}\n", False),
    ("comment_unclosed_for", "{% comment %}{% for a in b %}{% endcomment %}\n", True),
    ("comment_unclosed_capture", "{% comment %}{% capture v %}{% endcomment %}\n", True),
    ("comment_unclosed_raw", "{% comment %}{% raw %}x{% endcomment %}\n", True),
    (
        "comment_closed_raw",
        "{% comment %}{% raw %}{{n=2}{% endraw %}{% endcomment %}\n",
        False,
    ),
    (
        "comment_nested_comment",
        "{% comment %}{% comment %}x{% endcomment %}{% endcomment %}\n",
        False,
    ),
    (
        "comment_nested_unclosed",
        "{% comment %}{% comment %}x{% endcomment %}\n",
        True,
    ),
    (
        "comment_unknown_inside_if",
        "{% comment %}{% if x %}{% nope %}{% endif %}{% endcomment %}\n",
        True,
    ),
    (
        "comment_badvar_inside_if",
        "{% comment %}{% if x %}{{n=2}{% endif %}{% endcomment %}\n",
        True,
    ),
    (
        "comment_crossed_nesting",
        "{% comment %}{% if x %}{% endfor %}{% endif %}{% endcomment %}\n",
        True,
    ),
    ("comment_assign", "{% comment %}{% assign x = 1 %}{% endcomment %}\n", False),
    ("unknown_inside_if", "{% if x %}{% nope %}{% endif %}\n", True),
)


# Shapes that abort a real Pages build but that this guard deliberately does not
# detect, because catching them means resolving `_includes/` and the site's
# document set rather than tokenising Liquid. Pinned so the gap stays visible: if
# coverage is ever added, this test fails and someone updates it on purpose.
UNCOVERED_CASES = (
    ("include_missing", "{% include nope.html %}\n"),
    ("link_missing", "{% link nope.md %}\n"),
)


class UncoveredCaseTests(unittest.TestCase):
    def test_resource_resolution_is_a_known_gap(self) -> None:
        """These fail a real build; the guard is syntax-only and says so."""
        for label, markdown in UNCOVERED_CASES:
            with self.subTest(label=label):
                self.assertEqual(
                    [], check_jekyll_liquid.scan_text(markdown, "sample.md")
                )


class FatalityTests(unittest.TestCase):
    def test_verdicts_match_a_real_jekyll_build(self) -> None:
        """Each row's expectation is Jekyll's observed exit code, not a guess."""
        for label, markdown, fatal in FATALITY_CASES:
            with self.subTest(label=label):
                findings = check_jekyll_liquid.scan_text(markdown, "sample.md")

                self.assertEqual(
                    fatal,
                    bool(findings),
                    f"{label}: {[finding.message for finding in findings]}",
                )

    def test_block_tables_cover_every_end_tag(self) -> None:
        """An opener with no matching `end*` handling would never close."""
        for opener in check_jekyll_liquid.BLOCK_TAGS:
            with self.subTest(opener=opener):
                markdown = f"{{% {opener} x %}}body{{% end{opener} %}}\n"
                self.assertEqual([], check_jekyll_liquid.scan_text(markdown, "s.md"))

    def test_inner_tags_are_never_also_standalone(self) -> None:
        """Overlap would let an orphan inner tag pass as globally registered."""
        self.assertEqual(
            frozenset(),
            check_jekyll_liquid.INNER_TAGS & check_jekyll_liquid.STANDALONE_TAGS,
        )
        self.assertEqual(
            frozenset(),
            frozenset(check_jekyll_liquid.BLOCK_TAGS) & check_jekyll_liquid.STANDALONE_TAGS,
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

    def test_rendered_suffixes_cover_every_jekyll_markdown_extension(self) -> None:
        """A narrower set would be a silent hole, which is the one thing this
        guard must not have."""
        self.assertEqual(
            {".markdown", ".mkdown", ".mkdn", ".mkd", ".md"},
            set(check_jekyll_liquid.load_rendered_suffixes()),
        )

    def test_rendered_suffixes_honour_a_config_override(self) -> None:
        with TemporaryDirectory() as temporary_directory:
            config = Path(temporary_directory) / "_config.yml"
            config.write_text("markdown_ext: md, text\n", encoding="utf-8")

            self.assertEqual(
                {".md", ".text"},
                set(check_jekyll_liquid.load_rendered_suffixes(config)),
            )

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
