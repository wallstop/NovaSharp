#!/usr/bin/env python3
"""Focused checks for Lua fixture metadata used by comparison CI."""

from __future__ import annotations

import sys
import unittest
import json
from collections import Counter
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / "tools" / "LuaCorpusExtractor"))
FIXTURES_DIR = (
    ROOT
    / "src"
    / "tests"
    / "WallstopStudios.NovaSharp.Interpreter.Tests"
    / "LuaFixtures"
)
MANIFEST = FIXTURES_DIR / "manifest.json"


def read_header(relative_path: str) -> tuple[dict[str, str], str]:
    text = (FIXTURES_DIR / relative_path).read_text(encoding="utf-8")
    metadata: dict[str, str] = {}
    for line in text.splitlines()[:10]:
        if not line.startswith("--"):
            break
        line = line[2:].strip()
        if not line.startswith("@") or ":" not in line:
            continue
        key, value = line.split(":", 1)
        metadata[key.lower()] = value.strip()
    return metadata, text


def read_manifest_entries() -> list[dict[str, object]]:
    with MANIFEST.open(encoding="utf-8") as manifest_file:
        manifest = json.load(manifest_file)

    return manifest["snippets"]


def read_manifest_by_path() -> dict[str, dict[str, object]]:
    return {entry["path"]: entry for entry in read_manifest_entries()}


def duplicate_paths(paths: list[str]) -> list[str]:
    return sorted(path for path, count in Counter(paths).items() if count > 1)


class LuaFixtureMetadataTests(unittest.TestCase):
    def test_registered_basic_callback_fixtures_cover_their_source_versions(self) -> None:
        common, _ = read_header(
            "BasicModuleTUnitTests/RegisteredBasicCallbacksUseArgumentViews.lua"
        )
        warning, warning_text = read_header(
            "BasicModuleTUnitTests/RegisteredBasicCallbacksUseArgumentViews_2.lua"
        )

        self.assertEqual("5.1+", common.get("@lua-versions"))
        self.assertEqual("false", common.get("@novasharp-only"))
        self.assertEqual("5.4, 5.5", warning.get("@lua-versions"))
        self.assertEqual("false", warning.get("@novasharp-only"))
        self.assertIn("warn('@on')", warning_text)
        self.assertIn("warn('@off')", warning_text)

    def test_extracted_source_manifest_and_fixture_paths_match(self) -> None:
        """Every extracted snippet must have exactly one manifest entry and fixture."""
        import lua_corpus_extractor_v2 as extractor

        result = extractor.extract_all_snippets(extractor.DEFAULT_TEST_DIRS)
        extractor.reconcile_snippet_output_paths(result, FIXTURES_DIR)
        self.assertEqual([], result.errors, "Lua source extraction reported errors")

        extracted_paths = [snippet.output_path for snippet in result.snippets]
        manifest_paths = [str(entry["path"]) for entry in read_manifest_entries()]
        fixture_paths = sorted(
            path.relative_to(FIXTURES_DIR).as_posix()
            for path in FIXTURES_DIR.rglob("*.lua")
        )

        extracted = set(extracted_paths)
        manifest = set(manifest_paths)
        fixtures = set(fixture_paths)
        mismatches = []

        for label, paths in (
            ("source", extracted_paths),
            ("manifest", manifest_paths),
            ("fixtures", fixture_paths),
        ):
            duplicates = duplicate_paths(paths)
            if duplicates:
                mismatches.append(f"duplicate {label} paths: {duplicates[:10]}")

        comparisons = (
            ("missing from manifest", extracted - manifest),
            ("not extracted from source but listed in manifest", manifest - extracted),
            ("missing fixture", extracted - fixtures),
        )
        for label, paths in comparisons:
            if paths:
                mismatches.append(f"{label}: {sorted(paths)[:10]}")

        self.assertEqual([], mismatches, "\n".join(mismatches))

    def test_interop_equality_injected_userdata_fixtures_are_novasharp_only(
        self,
    ) -> None:
        for relative_path in (
            "ArithmOperatorsTestClass/InteropMetaEquality.lua",
            "ArithmOperatorsTestClass/InteropMetaEquality_4.lua",
        ):
            with self.subTest(relative_path=relative_path):
                metadata, text = read_header(relative_path)

                self.assertEqual("novasharp-only", metadata.get("@lua-versions"))
                self.assertEqual("true", metadata.get("@novasharp-only"))
                self.assertIn("Uses injected variable: o1", text)

    def test_interop_equality_self_comparison_does_not_expect_error(self) -> None:
        metadata, _ = read_header("ArithmOperatorsTestClass/InteropMetaEquality.lua")

        self.assertEqual("false", metadata.get("@expects-error"))

    def test_interop_equality_manifest_matches_fixture_metadata(self) -> None:
        manifest = read_manifest_by_path()

        for relative_path in (
            "ArithmOperatorsTestClass/InteropMetaEquality.lua",
            "ArithmOperatorsTestClass/InteropMetaEquality_4.lua",
        ):
            with self.subTest(relative_path=relative_path):
                entry = manifest[relative_path]

                self.assertTrue(entry["novasharp_only"])
                self.assertFalse(entry["expects_error"])

    def test_manifest_agrees_with_every_fixture_header(self) -> None:
        """The manifest is generated from the headers, so it must match them.

        It silently drifted instead: the extractor recomputed metadata for the
        manifest while leaving curated headers on disk, so entries such as
        `MyObject/IndexSetDoesNotWrackStack.lua` reported `novasharp_only: false`
        against a header that says `true`.
        """
        import lua_corpus_extractor_v2 as extractor

        mismatches = []
        for relative_path, entry in sorted(read_manifest_by_path().items()):
            fixture = FIXTURES_DIR / relative_path
            if not fixture.exists():
                mismatches.append(f"{relative_path}: listed in manifest but missing")
                continue

            metadata, _ = read_header(relative_path)
            compatibility = extractor.compatibility_from_metadata(metadata)
            if compatibility is None:
                mismatches.append(f"{relative_path}: header has no version metadata")
                continue

            expected = {
                "lua_versions": compatibility.compatible_versions,
                "novasharp_only": compatibility.novasharp_only,
                "expects_error": metadata.get("@expects-error", "").lower() == "true",
            }
            actual = {key: entry[key] for key in expected}
            if actual != expected:
                mismatches.append(f"{relative_path}: header {expected} != manifest {actual}")

        self.assertEqual([], mismatches[:20], f"{len(mismatches)} fixture(s) drifted")


if __name__ == "__main__":
    unittest.main()
