#!/usr/bin/env python3
"""Regression tests for curated-metadata preservation in the Lua corpus extractor.

`@lua-versions`, `@novasharp-only`, and `@expects-error` are curated by hand
against reference Lua and cannot be rediscovered by the extractor's heuristics.
Regeneration used to recompute them, silently reintroducing divergences that had
already been resolved. These tests pin the preservation contract.
"""

from __future__ import annotations

import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

import lua_corpus_extractor_v2 as extractor


# (label, header value, expected compatible versions, expected novasharp_only)
COMPATIBILITY_CASES = (
    ("all_versions_shorthand", "5.1+", ["5.1", "5.2", "5.3", "5.4", "5.5"], False),
    ("open_ended_range", "5.3+", ["5.3", "5.4", "5.5"], False),
    ("explicit_list", "5.2, 5.4", ["5.2", "5.4"], False),
    ("closed_range", "5.2-5.4", ["5.2", "5.3", "5.4"], False),
    ("novasharp_only", "novasharp-only", [], True),
    ("no_versions", "none", [], False),
)


def make_snippet(
    *,
    lua_code: str = "return 1",
    test_class: str = "SampleTests",
    test_method: str = "Sample",
    line_number: int = 10,
    compatibility: extractor.LuaVersionCompatibility | None = None,
    expects_error: bool = False,
) -> extractor.LuaSnippet:
    return extractor.LuaSnippet(
        lua_code=lua_code,
        source_file="src/tests/Sample.cs",
        line_number=line_number,
        test_class=test_class,
        test_method=test_method,
        compatibility=compatibility or extractor.LuaVersionCompatibility(),
        expects_error=expects_error,
    )


class RewriteCuratedHeaderTests(unittest.TestCase):
    def test_refreshes_only_tool_owned_keys(self) -> None:
        header = [
            "-- @lua-versions: 5.3+",
            "-- @novasharp-only: true",
            "-- @expects-error: false",
            "-- @source: src/tests/Old.cs:1",
            "-- @test: Old.Method",
            "-- Table iteration order is implementation-defined",
        ]

        rewritten = extractor.rewrite_curated_header(
            header,
            {"@source": "src/tests/New.cs:42", "@test": "New.Method"},
        )

        self.assertEqual(
            [
                "-- @lua-versions: 5.3+",
                "-- @novasharp-only: true",
                "-- @expects-error: false",
                "-- @source: src/tests/New.cs:42",
                "-- @test: New.Method",
                "-- Table iteration order is implementation-defined",
            ],
            rewritten,
        )

    def test_preserves_unrecognised_keys_and_order(self) -> None:
        header = [
            "-- @novasharp-only: true",
            "-- @future-key: keep me",
            "-- @lua-versions: 5.1+",
        ]

        rewritten = extractor.rewrite_curated_header(
            header, {"@source": "a.cs:1", "@test": "T.M"}
        )

        self.assertEqual(header, rewritten[:3])
        self.assertEqual(["-- @source: a.cs:1", "-- @test: T.M"], rewritten[3:])

    def test_appends_missing_keys_after_the_last_key_line(self) -> None:
        header = ["-- @lua-versions: 5.1+", "-- a trailing note"]

        rewritten = extractor.rewrite_curated_header(
            header, {"@source": "a.cs:1", "@test": "T.M"}
        )

        self.assertEqual(
            [
                "-- @lua-versions: 5.1+",
                "-- @source: a.cs:1",
                "-- @test: T.M",
                "-- a trailing note",
            ],
            rewritten,
        )


class CompatibilityFromMetadataTests(unittest.TestCase):
    def test_parses_every_curated_version_form(self) -> None:
        for label, value, versions, novasharp_only in COMPATIBILITY_CASES:
            with self.subTest(label=label):
                compatibility = extractor.compatibility_from_metadata(
                    {"@lua-versions": value}
                )

                self.assertIsNotNone(compatibility)
                assert compatibility is not None
                self.assertEqual(versions, compatibility.compatible_versions)
                self.assertEqual(novasharp_only, compatibility.novasharp_only)

    def test_round_trips_through_the_emitted_header_value(self) -> None:
        for label, value, _, _ in COMPATIBILITY_CASES:
            with self.subTest(label=label):
                first = extractor.compatibility_from_metadata({"@lua-versions": value})
                assert first is not None
                second = extractor.compatibility_from_metadata(
                    {"@lua-versions": first.version_string}
                )

                assert second is not None
                self.assertEqual(first.compatible_versions, second.compatible_versions)
                self.assertEqual(first.novasharp_only, second.novasharp_only)

    def test_returns_none_without_curated_version_keys(self) -> None:
        self.assertIsNone(
            extractor.compatibility_from_metadata({"@expects-error": "true"})
        )

    def test_novasharp_only_flag_wins_over_the_version_list(self) -> None:
        compatibility = extractor.compatibility_from_metadata(
            {"@lua-versions": "5.1+", "@novasharp-only": "true"}
        )

        assert compatibility is not None
        self.assertTrue(compatibility.novasharp_only)
        self.assertEqual([], compatibility.compatible_versions)
        self.assertEqual("novasharp-only", compatibility.version_string)


class StripAbsorbedBodyPrefixTests(unittest.TestCase):
    def test_drops_snippet_comments_mistaken_for_header(self) -> None:
        header = [
            "-- @lua-versions: 5.1+",
            "-- @test: T.M",
            "-- defines a factorial function",
        ]

        self.assertEqual(
            header[:2],
            extractor.strip_absorbed_body_prefix(
                header, "-- defines a factorial function\nreturn 1"
            ),
        )

    def test_keeps_curated_notes_that_are_not_body_comments(self) -> None:
        header = ["-- @lua-versions: 5.1+", "-- Uses injected variable: o1"]

        self.assertEqual(
            header, extractor.strip_absorbed_body_prefix(header, "return o1 == o1")
        )

    def test_drops_every_absorbed_comment_line(self) -> None:
        header = ["-- @test: T.M", "-- first", "-- second"]

        self.assertEqual(
            header[:1],
            extractor.strip_absorbed_body_prefix(header, "-- first\n-- second\nreturn 1"),
        )


class ApplyCuratedMetadataTests(unittest.TestCase):
    CURATED_FIXTURE = (
        "-- @lua-versions: novasharp-only\n"
        "-- @novasharp-only: true\n"
        "-- @expects-error: false\n"
        "-- @source: src/tests/Stale.cs:1\n"
        "-- @test: SampleTests.Sample\n"
        "-- Uses injected variable: o1\n"
        "return o1 == o1\n"
    )

    def _write_fixture(self, directory: Path, content: str) -> None:
        target = directory / "SampleTests"
        target.mkdir(parents=True, exist_ok=True)
        (target / "Sample.lua").write_text(content, encoding="utf-8")

    def test_curated_values_beat_recomputed_values(self) -> None:
        snippet = make_snippet(
            lua_code="return o1 == o1",
            compatibility=extractor.LuaVersionCompatibility(),
            expects_error=True,
        )
        result = extractor.ExtractionResult(snippets=[snippet])

        with TemporaryDirectory() as temporary_directory:
            output_dir = Path(temporary_directory)
            self._write_fixture(output_dir, self.CURATED_FIXTURE)

            overrides = extractor.apply_curated_metadata(result, output_dir)

        self.assertTrue(snippet.compatibility.novasharp_only)
        self.assertFalse(snippet.expects_error)
        self.assertEqual(
            {"@novasharp-only", "@lua-versions", "@expects-error"},
            {override.key for override in overrides},
        )

    def test_regeneration_only_rewrites_source_and_test(self) -> None:
        snippet = make_snippet(
            lua_code="return o1 == o1",
            line_number=99,
            expects_error=True,
        )
        result = extractor.ExtractionResult(snippets=[snippet])

        with TemporaryDirectory() as temporary_directory:
            output_dir = Path(temporary_directory)
            self._write_fixture(output_dir, self.CURATED_FIXTURE)

            extractor.apply_curated_metadata(result, output_dir)
            extractor.write_snippets(result, output_dir)
            regenerated = (output_dir / "SampleTests" / "Sample.lua").read_text(
                encoding="utf-8"
            )

        self.assertEqual(
            self.CURATED_FIXTURE.replace(
                "-- @source: src/tests/Stale.cs:1",
                "-- @source: src/tests/Sample.cs:99",
            ),
            regenerated,
        )

    def test_regeneration_is_idempotent(self) -> None:
        with TemporaryDirectory() as temporary_directory:
            output_dir = Path(temporary_directory)
            self._write_fixture(output_dir, self.CURATED_FIXTURE)

            contents = []
            for _ in range(3):
                snippet = make_snippet(lua_code="return o1 == o1", line_number=99)
                result = extractor.ExtractionResult(snippets=[snippet])
                extractor.apply_curated_metadata(result, output_dir)
                extractor.write_snippets(result, output_dir)
                extractor.write_manifest(result, output_dir)
                contents.append(
                    (
                        (output_dir / "SampleTests" / "Sample.lua").read_text(
                            encoding="utf-8"
                        ),
                        (output_dir / "manifest.json").read_text(encoding="utf-8"),
                    )
                )

        self.assertEqual(contents[0], contents[1])
        self.assertEqual(contents[1], contents[2])

    def test_body_comments_are_not_duplicated_on_regeneration(self) -> None:
        lua_code = "-- defines a factorial function\nreturn 1"
        fixture = (
            "-- @lua-versions: 5.1+\n"
            "-- @novasharp-only: false\n"
            "-- @expects-error: false\n"
            "-- @source: src/tests/Sample.cs:99\n"
            "-- @test: SampleTests.Sample\n"
            f"{lua_code}\n"
        )

        with TemporaryDirectory() as temporary_directory:
            output_dir = Path(temporary_directory)
            self._write_fixture(output_dir, fixture)

            snippet = make_snippet(lua_code=lua_code, line_number=99)
            result = extractor.ExtractionResult(snippets=[snippet])
            extractor.apply_curated_metadata(result, output_dir)
            extractor.write_snippets(result, output_dir)
            regenerated = (output_dir / "SampleTests" / "Sample.lua").read_text(
                encoding="utf-8"
            )

        self.assertEqual(fixture, regenerated)

    def test_manifest_matches_the_curated_header(self) -> None:
        snippet = make_snippet(lua_code="return o1 == o1", expects_error=True)
        result = extractor.ExtractionResult(snippets=[snippet])

        with TemporaryDirectory() as temporary_directory:
            output_dir = Path(temporary_directory)
            self._write_fixture(output_dir, self.CURATED_FIXTURE)

            extractor.apply_curated_metadata(result, output_dir)
            extractor.write_manifest(result, output_dir)
            manifest = (output_dir / "manifest.json").read_text(encoding="utf-8")

        self.assertIn('"novasharp_only": true', manifest)
        self.assertIn('"expects_error": false', manifest)


class SourcePathTests(unittest.TestCase):
    def test_source_paths_use_posix_separators(self) -> None:
        """Native separators made every cross-platform regeneration churn."""
        for snippet in extractor.extract_all_snippets(
            [extractor.ROOT / "src" / "tests"]
        ).snippets[:50]:
            with self.subTest(source=snippet.source_file):
                self.assertNotIn("\\", snippet.source_file)


if __name__ == "__main__":
    unittest.main()
