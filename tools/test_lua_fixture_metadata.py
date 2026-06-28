#!/usr/bin/env python3
"""Focused checks for Lua fixture metadata used by comparison CI."""

from __future__ import annotations

import unittest
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
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


def read_manifest_by_path() -> dict[str, dict[str, object]]:
    with MANIFEST.open(encoding="utf-8") as manifest_file:
        manifest = json.load(manifest_file)

    return {entry["path"]: entry for entry in manifest["snippets"]}


class LuaFixtureMetadataTests(unittest.TestCase):
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


if __name__ == "__main__":
    unittest.main()
