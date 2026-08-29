#!/usr/bin/env python3
"""Extract Lua snippets from NovaSharp C# test files with version compatibility metadata.

This tool parses C# test files looking for `DoString(...)` calls and extracts
the Lua code from string literals. The extracted snippets are written to
`src/tests/NovaSharp.Interpreter.Tests/LuaFixtures/` with version compatibility
headers so they can be tested against real Lua runtimes.

Each extracted file includes a metadata header:
    -- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
    -- @novasharp-only: false
    -- @source: path/to/test.cs:123
    -- @test: TestClass.TestMethod

Curated metadata is authoritative
---------------------------------
`@lua-versions`, `@novasharp-only`, and `@expects-error` are decided by a human
against reference Lua and are the only three keys the comparison harness reads.
The heuristics in this file cannot rediscover those decisions, so for a fixture
that already exists on disk the committed header wins: only `@source`, `@test`,
and the snippet body are refreshed, and curated comment lines are kept verbatim.
Pass `--refresh-metadata` to deliberately recompute instead — expect to re-audit
every fixture it changes.

Usage:
    python tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py
    python tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py --dry-run
    python tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py --output-dir custom/path
    python tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py --refresh-metadata
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterator

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUTPUT_DIR = ROOT / "src" / "tests" / "WallstopStudios.NovaSharp.Interpreter.Tests" / "LuaFixtures"
DEFAULT_TEST_DIRS = [
    ROOT / "src" / "tests" / "WallstopStudios.NovaSharp.Interpreter.Tests.TUnit",
    ROOT / "src" / "tests" / "WallstopStudios.NovaSharp.Interpreter.Tests",
]

sys.path.insert(0, str(ROOT / "tools"))
from lua_version_utils import ALL_LUA_VERSIONS, parse_lua_versions  # noqa: E402

# Header keys a human curates against reference Lua. These are exactly the keys
# `scripts/tests/compare-lua-outputs.py` reads, and the only ones this tool must
# never overwrite on an existing fixture.
CURATED_KEYS = ("@lua-versions", "@novasharp-only", "@expects-error")
# Keys this tool owns and always refreshes from the test source.
REFRESHED_KEYS = ("@source", "@test")

# Pattern to match DoString calls with various string literal forms
DOSTRING_CALL_PATTERN = re.compile(
    r'\.DoString\s*\(\s*'
    r'(?:'
    r'@"(?P<verbatim>(?:[^"]|"")*)"|'  # Verbatim string @"..."
    r'"(?P<regular>(?:[^"\\]|\\.)*)"|'  # Regular string "..."
    r'"""(?P<raw>.*?)"""|'  # Raw string literal """..."""
    r'\$@"(?P<interp_verbatim>(?:[^"]|"")*)"|'  # Interpolated verbatim $@"..."
    r'\$"(?P<interp>(?:[^"\\]|\\.)*)"|'  # Interpolated $"..."
    r'(?P<variable>\w+)'  # Variable reference
    r')',
    re.DOTALL
)

# Pattern to find variable assignments like: string code = @"...";
# Supports verbatim, regular, and raw string literals
VAR_ASSIGNMENT_PATTERN = re.compile(
    r'(?:string|var)\s+(?P<varname>\w+)\s*=\s*'
    r'(?:'
    r'@"(?P<verbatim>(?:[^"]|"")*)"|'  # Verbatim string @"..."
    r'"(?P<regular>(?:[^"\\]|\\.)*)"|'  # Regular string "..."
    r'"""(?P<raw>.*?)"""|'  # Raw string literal """..."""
    r'\$@"(?P<interp_verbatim>(?:[^"]|"")*)"|'  # Interpolated verbatim $@"..."
    r'\$"(?P<interp>(?:[^"\\]|\\.)*)"'  # Interpolated $"..."
    r')\s*;',
    re.DOTALL
)

# Pattern to match test method declarations
# Matches [Test], [TUnit.Core.Test], [global::TUnit.Core.Test]
TEST_METHOD_PATTERN = re.compile(
    r'\[(?:global::)?(?:TUnit\.Core\.)?Test\].*?'
    r'(?:public\s+)?(?:async\s+)?(?:Task|void)\s+(\w+)\s*\(',
    re.DOTALL
)

# Pattern to match test class declarations
TEST_CLASS_PATTERN = re.compile(
    r'(?:public\s+)?(?:sealed\s+)?class\s+(\w+)'
)

# Lua version feature detection patterns
# NOTE: goto and labels were introduced in Lua 5.2, not 5.4!
LUA_54_FEATURES = [
    (r'<const>', 'const attribute'),
    (r'<close>', 'close attribute'),
    (r'\bwarn\s*\(', 'warn function'),
]

LUA_53_FEATURES = [
    (r'(?<!/)//', 'floor division'),  # Match // but not in comments (preceded by /)
    # Note: removed the incorrect bitwise operators pattern that matched regular assignments
    (r'&(?![&])', 'bitwise AND'),
    (r'\|(?!\|)', 'bitwise OR'),
    (r'(?<![~=<>])~(?!=)', 'bitwise XOR/NOT'),  # ~ but not part of ~= or <=/>=/==
    (r'<<|>>', 'bit shift'),
    (r'utf8\.', 'utf8 library'),
    (r'table\.move\s*\(', 'table.move'),
    (r'math\.tointeger\s*\(', 'math.tointeger (5.3+)'),
    (r'math\.type\s*\(', 'math.type (5.3+)'),
    (r'math\.ult\s*\(', 'math.ult (5.3+)'),
    (r'math\.maxinteger\b', 'math.maxinteger (5.3+)'),
    (r'math\.mininteger\b', 'math.mininteger (5.3+)'),
    (r'string\.pack\s*\(', 'string.pack (5.3+)'),
    (r'string\.unpack\s*\(', 'string.unpack (5.3+)'),
    (r'string\.packsize\s*\(', 'string.packsize (5.3+)'),
]

LUA_52_FEATURES = [
    (r'goto\s+\w+', 'goto statement (5.2+)'),
    (r'::\w+::', 'label (5.2+)'),
    # Note: bit32 is handled separately in LUA_52_ONLY_FEATURES (removed in 5.3+)
    (r'_ENV\b', '_ENV variable'),
    (r'package\.searchpath', 'package.searchpath'),
    (r'rawlen\s*\(', 'rawlen function'),
    (r'table\.pack\s*\(', 'table.pack (5.2+)'),
    (r'table\.unpack\s*\(', 'table.unpack (5.2+)'),
    (r'debug\.upvalueid\s*\(', 'debug.upvalueid (5.2+)'),
    (r'debug\.upvaluejoin\s*\(', 'debug.upvaluejoin (5.2+)'),
    (r'debug\.getuservalue\s*\(', 'debug.getuservalue (5.2+)'),
    (r'debug\.setuservalue\s*\(', 'debug.setuservalue (5.2+)'),
    # debug.getlocal with function reference: matches func keyword or common var names (not just digits)
    (r'debug\.getlocal\s*\(\s*function', 'debug.getlocal with function arg (5.2+)'),
    (r'debug\.getlocal\s*\(\s*[a-zA-Z_][a-zA-Z0-9_]*\s*,', 'debug.getlocal with function var (5.2+)'),
    # debug.traceback with nil level: Lua 5.1 errors, 5.2+ accepts nil as default
    (r'debug\.traceback\s*\([^)]*,\s*nil\s*\)', 'debug.traceback with nil level (5.2+)'),
    # load with string argument: In Lua 5.1, load only accepts a reader function, not a string
    # Use loadstring in 5.1, or load(string) in 5.2+
    (r"\bload\s*\(\s*'", "load with string arg (5.2+)"),
    (r'\bload\s*\(\s*"', "load with string arg (5.2+)"),
    (r'\bload\s*\(\s*\[\[', "load with string arg (5.2+)"),
    (r'0[xX][0-9a-fA-F]*\.[0-9a-fA-F]', 'hex float literal (5.2+)'),
    (r'0[xX][0-9a-fA-F]+[pP]', 'hex float with exponent (5.2+)'),
]

LUA_51_INCOMPATIBLE = [
    # Features that don't work in Lua 5.1
    (r'\b//\b', 'floor division'),
    (r'&(?![&])', 'bitwise AND'),
    (r'\|(?!\|)', 'bitwise OR'),
    (r'goto\s+\w+', 'goto (5.2+)'),
    (r'::\w+::', 'label (5.2+)'),
    (r'<const>', 'const attribute'),
    (r'<close>', 'close attribute'),
    (r'math\.tointeger\s*\(', 'math.tointeger (5.3+)'),
    (r'math\.type\s*\(', 'math.type (5.3+)'),
    (r'math\.ult\s*\(', 'math.ult (5.3+)'),
    (r'utf8\.', 'utf8 library'),
    (r'table\.move\s*\(', 'table.move (5.3+)'),
    (r'table\.pack\s*\(', 'table.pack (5.2+)'),
    (r'table\.unpack\s*\(', 'table.unpack (5.2+)'),
    (r'rawlen\s*\(', 'rawlen (5.2+)'),
    (r'debug\.getlocal\s*\(\s*function', 'debug.getlocal with function arg (5.2+)'),
    (r'debug\.getlocal\s*\(\s*[a-zA-Z_][a-zA-Z0-9_]*\s*,', 'debug.getlocal with function var (5.2+)'),
    (r'debug\.upvalueid\s*\(', 'debug.upvalueid (5.2+)'),
    (r'debug\.upvaluejoin\s*\(', 'debug.upvaluejoin (5.2+)'),
    (r'debug\.getuservalue\s*\(', 'debug.getuservalue (5.2+)'),
    (r'debug\.setuservalue\s*\(', 'debug.setuservalue (5.2+)'),
    (r'debug\.traceback\s*\([^)]*,\s*nil\s*\)', 'debug.traceback with nil level (5.2+)'),
    # load with string argument: In Lua 5.1, load only accepts a reader function
    (r"\bload\s*\(\s*'", "load with string arg (5.2+)"),
    (r'\bload\s*\(\s*"', "load with string arg (5.2+)"),
    (r'\bload\s*\(\s*\[\[', "load with string arg (5.2+)"),
    (r'0[xX][0-9a-fA-F]*\.[0-9a-fA-F]', 'hex float literal (5.2+)'),
    (r'0[xX][0-9a-fA-F]+[pP]', 'hex float with exponent (5.2+)'),
]

# Functions deprecated or changed between Lua versions
LUA_51_ONLY_FEATURES = [
    # These exist in 5.1 but were removed or changed in later versions
    (r'table\.getn\s*\(', 'table.getn (5.1 only, deprecated)'),
    (r'table\.setn\s*\(', 'table.setn (5.1 only, deprecated)'),
    (r'math\.mod\s*\(', 'math.mod (5.1 only, use math.fmod)'),
    (r'string\.gfind\s*\(', 'string.gfind (5.1 only, use string.gmatch)'),
    (r'table\.foreach\s*\(', 'table.foreach (5.1 only, deprecated)'),
    (r'table\.foreachi\s*\(', 'table.foreachi (5.1 only, deprecated)'),
]

# Features that exist ONLY in Lua 5.2 (not 5.1, deprecated/removed in 5.3+)
LUA_52_ONLY_FEATURES = [
    # bit32 was added in 5.2 but deprecated and removed in 5.3+
    # (5.3+ uses native bitwise operators instead)
    (r'bit32\.', 'bit32 library (5.2 only, removed in 5.3+)'),
]

# Lua 5.5 specific features (currently Lua 5.5 is backward compatible with 5.4)
# As Lua 5.5 finalizes, add any 5.5-only features here
LUA_55_FEATURES = [
    # Lua 5.5 is still in development - features may be added here
    # Currently NovaSharp treats 5.5 as backward compatible with 5.4
    # (r'table\.create\s*\(', 'table.create (5.5+)'),  # Proposed feature
]

NOVASHARP_SPECIFIC_PATTERNS = [
    (r'\b!=\b', 'C-style not-equal'),
    (r'_NOVASHARP', 'NovaSharp global'),
    (r'_NovaSharp', 'NovaSharp global'),  # Both casings
    (r'clr\.', 'CLR interop'),
    (r'import\s*\(', 'NovaSharp import'),
    (r'dynamic\.', 'dynamic access'),
    (r'using\s+', 'using statement (non-Lua)'),
    (r'\{[a-zA-Z_][a-zA-Z0-9_]*\}', 'unresolved C# interpolation placeholder'),
    (r'json\.parse\s*\(', 'NovaSharp json module'),
    (r'json\.serialize\s*\(', 'NovaSharp json module'),
    (r'json\.isnull\s*\(', 'NovaSharp json module'),
    (r'json\.null\b', 'NovaSharp json module'),
    (r"require\s*\(\s*['\"]json['\"]\s*\)", 'NovaSharp json module'),
    (r'string\.startswith\s*\(', 'NovaSharp string extension'),
    (r'string\.endswith\s*\(', 'NovaSharp string extension'),
    (r':startsWith\s*\(', 'NovaSharp string extension (method-style)'),
    (r':endsWith\s*\(', 'NovaSharp string extension (method-style)'),
    (r':contains\s*\(', 'NovaSharp string extension (method-style)'),
    (r'string\.contains\s*\(', 'NovaSharp string extension'),
    (r'string\.unicode\s*\(', 'NovaSharp string extension'),
    (r'Script\.GlobalOptions', 'NovaSharp Script.GlobalOptions'),
    (r'sandbox', 'potential NovaSharp sandbox'),
    (r'debug\.debug\s*\(\s*\)', 'debug.debug() is interactive/platform-dependent'),
    # Metalua-style lambda syntax: |params|expression
    (r'\|[a-zA-Z_][a-zA-Z0-9_,\s]*\|', 'metalua-style lambda syntax'),
    # NovaSharp-specific error messages
    (r'CLR-call boundary', 'NovaSharp CLR-call boundary error message'),
    # NovaSharp prime table syntax: ${ key = value }
    (r'\$\s*\{', 'NovaSharp prime table syntax'),
]

# Patterns indicating the test expects an error
ERROR_EXPECTING_PATTERNS = [
    r'Assert\.Throws',
    r'Assert\.That\([^)]*Throws',
    r'Should\.Throw',
    r'ExpectedException',
    r'ShouldFail',
    r'ExpectedError',
]


@dataclass
class LuaVersionCompatibility:
    """Tracks which Lua versions a snippet is compatible with."""
    lua_51: bool = True
    lua_52: bool = True
    lua_53: bool = True
    lua_54: bool = True
    lua_55: bool = True
    novasharp_only: bool = False
    reasons: list[str] = field(default_factory=list)
    
    @property
    def compatible_versions(self) -> list[str]:
        """Return list of compatible Lua versions.

        A NovaSharp-only fixture has none: it is never run against a reference
        interpreter. Reporting versions here made the manifest disagree with the
        `novasharp-only` header it was generated from, so a regeneration that
        read that header back produced a different manifest.
        """
        if self.novasharp_only:
            return []

        versions = []
        if self.lua_51:
            versions.append("5.1")
        if self.lua_52:
            versions.append("5.2")
        if self.lua_53:
            versions.append("5.3")
        if self.lua_54:
            versions.append("5.4")
        if self.lua_55:
            versions.append("5.5")
        return versions
    
    @property
    def version_string(self) -> str:
        """Return comma-separated version string."""
        if self.novasharp_only:
            return "novasharp-only"
        versions = self.compatible_versions
        if not versions:
            return "none"
        if len(versions) == 5:
            return "5.1+"
        return ", ".join(versions)


@dataclass
class LuaSnippet:
    """Represents an extracted Lua snippet with metadata."""
    
    lua_code: str
    source_file: str
    line_number: int
    test_class: str
    test_method: str
    compatibility: LuaVersionCompatibility
    expects_error: bool = False
    snippet_index: int = 0
    # Header of the already-committed fixture, when one exists. Present means the
    # curated metadata in that header is authoritative for this snippet.
    curated_header_lines: list[str] | None = None

    @property
    def output_filename(self) -> str:
        """Generate the output filename for this snippet."""
        if self.snippet_index > 0:
            return f"{self.test_method}_{self.snippet_index}.lua"
        return f"{self.test_method}.lua"
    
    @property
    def output_path(self) -> str:
        """Generate the relative output path."""
        return f"{self.test_class}/{self.output_filename}"
    
    @property
    def refreshed_values(self) -> dict[str, str]:
        """Header values this tool owns and rewrites on every run."""
        return {
            "@source": f"{self.source_file}:{self.line_number}",
            "@test": f"{self.test_class}.{self.test_method}",
        }

    @property
    def emitted_header_values(self) -> dict[str, str]:
        """Return the tool-owned values that will actually be emitted.

        Existing fixtures keep a stable source line while they still point at
        the same source file. The manifest must use that preserved value too or
        regeneration immediately creates header/manifest drift.
        """
        if self.curated_header_lines is None:
            return self.refreshed_values

        rewritten = rewrite_curated_header(
            self.curated_header_lines, self.refreshed_values
        )
        metadata = parse_header_metadata(rewritten)
        return {
            key: metadata.get(key, value)
            for key, value in self.refreshed_values.items()
        }

    def generate_header(self) -> str:
        """Generate the metadata header for the Lua file.

        For a fixture that already exists, the committed header is reused with
        only `@source` / `@test` refreshed, so curated markers and hand-written
        compatibility notes survive regeneration.
        """
        if self.curated_header_lines is not None:
            lines = rewrite_curated_header(self.curated_header_lines, self.refreshed_values)
        else:
            lines = [
                f"-- @lua-versions: {self.compatibility.version_string}",
                f"-- @novasharp-only: {str(self.compatibility.novasharp_only).lower()}",
                f"-- @expects-error: {str(self.expects_error).lower()}",
                f"-- @source: {self.refreshed_values['@source']}",
                f"-- @test: {self.refreshed_values['@test']}",
            ]
            if self.compatibility.reasons:
                lines.append(f"-- Compatibility notes: {'; '.join(self.compatibility.reasons)}")
        lines.append("")
        return "\n".join(lines)


@dataclass
class ExtractionResult:
    """Result of extracting snippets from all test files."""
    
    snippets: list[LuaSnippet] = field(default_factory=list)
    errors: list[str] = field(default_factory=list)
    
    @property
    def total_snippets(self) -> int:
        return len(self.snippets)
    
    @property
    def novasharp_only_count(self) -> int:
        return sum(1 for s in self.snippets if s.compatibility.novasharp_only)
    
    @property
    def comparable_count(self) -> int:
        return sum(1 for s in self.snippets if not s.compatibility.novasharp_only)
    
    def by_version(self, version: str) -> list[LuaSnippet]:
        """Return snippets compatible with a specific Lua version."""
        return [s for s in self.snippets 
                if not s.compatibility.novasharp_only 
                and version in s.compatibility.compatible_versions]


def split_fixture_header(text: str) -> tuple[list[str], str]:
    """Split a fixture into its leading `--` comment header and its Lua body."""
    lines = text.splitlines()
    header_length = 0
    for line in lines:
        if not line.startswith("--"):
            break
        header_length += 1

    header = lines[:header_length]
    body = "\n".join(lines[header_length:])
    return header, body


def strip_absorbed_body_prefix(header_lines: list[str], lua_code: str) -> list[str]:
    """Remove snippet comments that `split_fixture_header` mistook for header.

    A fixture body may itself begin with unindented Lua comments, and those are
    indistinguishable from header lines by shape alone. The extracted snippet is
    the ground truth: any tail of `header_lines` that matches the snippet's own
    leading comments belongs to the body, not the header. Without this the
    comments are emitted twice, once more on every regeneration.

    Strips *every* accumulated copy, not just the last one. A fixture written by
    the pre-fix tool can already hold several, and removing one would leave the
    duplicate in place forever instead of healing it.
    """
    body_lines = lua_code.splitlines()
    leading = 0
    for line in body_lines:
        if not line.startswith("--"):
            break
        leading += 1

    if leading == 0:
        return header_lines

    remaining = list(header_lines)
    while True:
        for width in range(leading, 0, -1):
            if len(remaining) >= width and remaining[-width:] == body_lines[:width]:
                remaining = remaining[:-width]
                break
        else:
            return remaining


def fixture_body_matches(
    header_lines: list[str], body: str, lua_code: str
) -> bool:
    """Whether a fixture body equals a snippet, including absorbed comments.

    ``split_fixture_header`` necessarily treats leading Lua comments as header
    lines. Reattach the tail that matches the snippet before comparing bodies so
    comment-led fixtures do not acquire a new numeric suffix on every run.
    """
    curated_header = strip_absorbed_body_prefix(header_lines, lua_code)
    leading_body_comments: list[str] = []
    if len(curated_header) < len(header_lines):
        for line in lua_code.splitlines():
            if not line.startswith("--"):
                break
            leading_body_comments.append(line)

    reconstructed = "\n".join([*leading_body_comments, body]).strip()
    return reconstructed == lua_code


def parse_header_metadata(header_lines: list[str]) -> dict[str, str]:
    """Return the `@key: value` pairs in a fixture header, keyed lowercase."""
    metadata: dict[str, str] = {}
    for line in header_lines:
        stripped = line[2:].strip()
        if not stripped.startswith("@") or ":" not in stripped:
            continue
        key, value = stripped.split(":", 1)
        metadata[key.strip().lower()] = value.strip()
    return metadata


def rewrite_curated_header(header_lines: list[str], refreshed: dict[str, str]) -> list[str]:
    """Return `header_lines` with only the tool-owned keys refreshed.

    Curated keys, hand-written notes, ordering, and any unrecognised `@key` are
    preserved exactly. A tool-owned key missing from the committed header is
    appended after the last `@key` line so old fixtures gain it without churn.
    """
    rewritten: list[str] = []
    seen: set[str] = set()
    last_key_index = -1

    for line in header_lines:
        stripped = line[2:].strip()
        if stripped.startswith("@") and ":" in stripped:
            key = stripped.split(":", 1)[0].strip().lower()
            if key in refreshed:
                current_value = stripped.split(":", 1)[1].strip()
                refreshed_value = refreshed[key]
                if key == "@source" and _same_source_file(
                    current_value, refreshed_value
                ):
                    refreshed_value = current_value
                line = f"-- {key}: {refreshed_value}"
                seen.add(key)
            last_key_index = len(rewritten)
        rewritten.append(line)

    missing = [f"-- {key}: {refreshed[key]}" for key in REFRESHED_KEYS if key not in seen]
    if missing:
        insert_at = last_key_index + 1 if last_key_index >= 0 else len(rewritten)
        rewritten[insert_at:insert_at] = missing

    return rewritten


def _same_source_file(current: str, refreshed: str) -> bool:
    """Whether two ``path:line`` source values refer to the same file."""
    if ":" not in current or ":" not in refreshed:
        return False
    return current.rsplit(":", 1)[0] == refreshed.rsplit(":", 1)[0]


def compatibility_from_metadata(metadata: dict[str, str]) -> LuaVersionCompatibility | None:
    """Build a compatibility record from curated header metadata.

    Returns None when the header carries neither curated version key, so callers
    can fall back to the computed value.
    """
    versions_text = metadata.get("@lua-versions")
    novasharp_only_text = metadata.get("@novasharp-only")
    if versions_text is None and novasharp_only_text is None:
        return None

    novasharp_only = False
    if novasharp_only_text is not None:
        novasharp_only = novasharp_only_text.strip().lower() == "true"
    elif versions_text is not None and "novasharp-only" in versions_text.lower():
        novasharp_only = True

    if versions_text is None:
        versions = list(ALL_LUA_VERSIONS)
    elif versions_text.strip().lower() == "none" or "novasharp-only" in versions_text.lower():
        versions = []
    else:
        versions = parse_lua_versions(versions_text)

    return LuaVersionCompatibility(
        lua_51="5.1" in versions,
        lua_52="5.2" in versions,
        lua_53="5.3" in versions,
        lua_54="5.4" in versions,
        lua_55="5.5" in versions,
        novasharp_only=novasharp_only,
    )


@dataclass(frozen=True)
class CuratedOverride:
    """One curated header value that differs from what the heuristics computed."""

    path: str
    key: str
    curated: str
    computed: str


def apply_curated_metadata(
    result: ExtractionResult, output_dir: Path
) -> list[CuratedOverride]:
    """Let committed fixture headers win over recomputed metadata.

    Runs before both `write_snippets` and `write_manifest` so the files on disk
    and the manifest always agree. Returns the curated-vs-computed divergences
    for reporting.
    """
    overrides: list[CuratedOverride] = []

    for snippet in result.snippets:
        header_lines = snippet.curated_header_lines
        if header_lines is None:
            existing = output_dir / snippet.output_path
            try:
                text = existing.read_text(encoding="utf-8")
            except (FileNotFoundError, NotADirectoryError):
                continue
            except (OSError, UnicodeDecodeError) as error:
                result.errors.append(f"{existing}: could not read curated header: {error}")
                continue

            header_lines, body = split_fixture_header(text)
            if not fixture_body_matches(header_lines, body, snippet.lua_code):
                continue
            header_lines = strip_absorbed_body_prefix(header_lines, snippet.lua_code)
        if not header_lines:
            continue

        metadata = parse_header_metadata(header_lines)
        snippet.curated_header_lines = header_lines

        curated_compatibility = compatibility_from_metadata(metadata)
        if curated_compatibility is not None:
            if curated_compatibility.version_string != snippet.compatibility.version_string:
                overrides.append(
                    CuratedOverride(
                        snippet.output_path,
                        "@lua-versions",
                        curated_compatibility.version_string,
                        snippet.compatibility.version_string,
                    )
                )
            if curated_compatibility.novasharp_only != snippet.compatibility.novasharp_only:
                overrides.append(
                    CuratedOverride(
                        snippet.output_path,
                        "@novasharp-only",
                        str(curated_compatibility.novasharp_only).lower(),
                        str(snippet.compatibility.novasharp_only).lower(),
                    )
                )
            snippet.compatibility = curated_compatibility

        expects_error_text = metadata.get("@expects-error")
        if expects_error_text is not None:
            curated_expects_error = expects_error_text.strip().lower() == "true"
            if curated_expects_error != snippet.expects_error:
                overrides.append(
                    CuratedOverride(
                        snippet.output_path,
                        "@expects-error",
                        str(curated_expects_error).lower(),
                        str(snippet.expects_error).lower(),
                    )
                )
            snippet.expects_error = curated_expects_error

    return overrides


def unescape_csharp_string(content: str, is_verbatim: bool = False) -> str:
    """Convert C# string literal escapes to actual characters."""
    if is_verbatim:
        return content.replace('""', '"')
    
    replacements = [
        ('\\n', '\n'),
        ('\\r', '\r'),
        ('\\t', '\t'),
        ('\\\\', '\\'),
        ('\\"', '"'),
        ("\\'", "'"),
        ('\\0', '\0'),
    ]
    result = content
    for old, new in replacements:
        result = result.replace(old, new)
    return result


def build_variable_lookup(content: str) -> dict[str, list[tuple[str, int]]]:
    """Build a lookup table of variable assignments: varname -> list of (lua_code, position).
    
    Returns all assignments for each variable name, sorted by position.
    This allows finding the closest preceding assignment for a DoString call.
    """
    variables: dict[str, list[tuple[str, int]]] = {}
    for match in VAR_ASSIGNMENT_PATTERN.finditer(content):
        varname = match.group('varname')
        position = match.start()
        
        # Extract the string content
        if match.group('verbatim') is not None:
            lua_code = unescape_csharp_string(match.group('verbatim'), is_verbatim=True)
        elif match.group('regular') is not None:
            lua_code = unescape_csharp_string(match.group('regular'), is_verbatim=False)
        elif match.group('raw') is not None:
            lua_code = match.group('raw')
        elif match.group('interp_verbatim') is not None:
            lua_code = unescape_csharp_string(match.group('interp_verbatim'), is_verbatim=True)
        elif match.group('interp') is not None:
            lua_code = unescape_csharp_string(match.group('interp'), is_verbatim=False)
        else:
            continue
        
        if varname not in variables:
            variables[varname] = []
        variables[varname].append((lua_code, position))
    
    # Sort each variable's assignments by position
    for varname in variables:
        variables[varname].sort(key=lambda x: x[1])
    
    return variables


def resolve_variable(varname: str, dostring_position: int, 
                     var_lookup: dict[str, list[tuple[str, int]]]) -> str | None:
    """Find the closest preceding variable assignment for a DoString call."""
    if varname not in var_lookup:
        return None
    
    assignments = var_lookup[varname]
    # Find the last assignment that comes before the DoString position
    best_match = None
    for lua_code, pos in assignments:
        if pos < dostring_position:
            best_match = lua_code
        else:
            break  # Assignments are sorted, so we can stop here
    
    return best_match


def extract_lua_from_match(match: re.Match) -> tuple[str, bool]:
    """Extract Lua code from a regex match, returning (code, is_variable)."""
    if match.group('verbatim') is not None:
        return unescape_csharp_string(match.group('verbatim'), is_verbatim=True), False
    if match.group('regular') is not None:
        return unescape_csharp_string(match.group('regular'), is_verbatim=False), False
    if match.group('raw') is not None:
        return match.group('raw'), False
    if match.group('interp_verbatim') is not None:
        return unescape_csharp_string(match.group('interp_verbatim'), is_verbatim=True), False
    if match.group('interp') is not None:
        return unescape_csharp_string(match.group('interp'), is_verbatim=False), False
    if match.group('variable') is not None:
        return match.group('variable'), True
    return "", True


def find_containing_class(content: str, position: int) -> str:
    """Find the class name containing the given position."""
    search_content = content[:position]
    matches = list(TEST_CLASS_PATTERN.finditer(search_content))
    if matches:
        return matches[-1].group(1)
    return "Unknown"


def find_containing_method(content: str, position: int) -> str:
    """Find the test method name containing the given position."""
    search_content = content[:position]
    matches = list(TEST_METHOD_PATTERN.finditer(search_content))
    if matches:
        return matches[-1].group(1)
    return "Unknown"


def count_lines_before(content: str, position: int) -> int:
    """Count the number of newlines before the given position."""
    return content[:position].count('\n') + 1


# Test class prefixes that indicate NovaSharp-only tests
NOVASHARP_ONLY_TEST_CLASS_PREFIXES = [
    'Sandbox',  # All sandbox tests use NovaSharp-specific sandbox functionality
    'DynamicUserData',  # Dynamic CLR interop
    'JsonModule',  # NovaSharp JSON module
    'IoModuleVirtualization',  # NovaSharp IO stream virtualization
    'OsSystemModule',  # os.execute behavior differs from standard Lua
    'OsExecuteVersionParity',  # Uses StubPlatformAccessor for command virtualization
    'Bit32CompatibilityWarning',  # Tests NovaSharp's bit32 compatibility warning behavior
]

# Test method substrings that indicate NovaSharp-only behavior
# These test methods verify NovaSharp-specific behavior that differs from reference Lua by design
NOVASHARP_ONLY_TEST_METHOD_SUBSTRINGS = [
    'IsUnsupported',  # Tests for intentionally unsupported features (e.g., io.popen)
    'SetDefaultFileOverridesStdOutStream',  # Requires C#-configured stdout stream
    'StdOutWritesHonorCustomScriptOptionStream',  # Requires C# ScriptOptions stdout stream
]


def analyze_version_compatibility(lua_code: str, surrounding_context: str, test_class: str = "", test_method: str = "") -> LuaVersionCompatibility:
    """Analyze Lua code to determine version compatibility."""
    compat = LuaVersionCompatibility()
    
    # Check if test class indicates NovaSharp-only functionality
    for prefix in NOVASHARP_ONLY_TEST_CLASS_PREFIXES:
        if test_class.startswith(prefix):
            compat.novasharp_only = True
            compat.reasons.append(f"Test class '{test_class}' uses NovaSharp-specific {prefix} functionality")
            return compat  # No need to check further - it's definitely NovaSharp-only
    
    # Check if test method name indicates NovaSharp-only behavior
    for substring in NOVASHARP_ONLY_TEST_METHOD_SUBSTRINGS:
        if substring in test_method:
            compat.novasharp_only = True
            compat.reasons.append(f"Test method '{test_method}' tests NovaSharp-specific behavior ({substring})")
            return compat  # No need to check further - it's definitely NovaSharp-only
    
    # Check for NovaSharp-specific patterns
    for pattern, reason in NOVASHARP_SPECIFIC_PATTERNS:
        if re.search(pattern, lua_code):
            compat.novasharp_only = True
            compat.reasons.append(f"NovaSharp: {reason}")
    
    # Check for explicit version requirements in test context
    if 'Lua51' in surrounding_context or 'CompatibilityVersion.Lua_5_1' in surrounding_context:
        compat.lua_52 = False
        compat.lua_53 = False
        compat.lua_54 = False
        compat.lua_55 = False
        compat.reasons.append("Test targets Lua 5.1")
    elif 'Lua52' in surrounding_context or 'CompatibilityVersion.Lua_5_2' in surrounding_context:
        compat.lua_51 = False
        compat.reasons.append("Test targets Lua 5.2+")
    elif 'Lua53' in surrounding_context or 'CompatibilityVersion.Lua_5_3' in surrounding_context:
        compat.lua_51 = False
        compat.lua_52 = False
        compat.reasons.append("Test targets Lua 5.3+")
    elif 'Lua54' in surrounding_context or 'CompatibilityVersion.Lua_5_4' in surrounding_context:
        compat.lua_51 = False
        compat.lua_52 = False
        compat.lua_53 = False
        compat.reasons.append("Test targets Lua 5.4+")
    elif 'Lua55' in surrounding_context or 'CompatibilityVersion.Lua_5_5' in surrounding_context:
        compat.lua_51 = False
        compat.lua_52 = False
        compat.lua_53 = False
        compat.lua_54 = False
        compat.reasons.append("Test targets Lua 5.5+")
    
    # If NovaSharp-only, skip further analysis
    if compat.novasharp_only:
        return compat
    
    # Check for Lua 5.4 specific features
    for pattern, reason in LUA_54_FEATURES:
        if re.search(pattern, lua_code):
            compat.lua_51 = False
            compat.lua_52 = False
            compat.lua_53 = False
            # Note: Lua 5.5 is expected to support 5.4 features (backward compatible)
            compat.reasons.append(f"Lua 5.4+: {reason}")
    
    # Check for Lua 5.3+ features (bitwise operators, floor division, utf8)
    for pattern, reason in LUA_53_FEATURES:
        if re.search(pattern, lua_code):
            compat.lua_51 = False
            compat.lua_52 = False
            compat.reasons.append(f"Lua 5.3+: {reason}")
    
    # Check for Lua 5.2+ features
    for pattern, reason in LUA_52_FEATURES:
        if re.search(pattern, lua_code):
            compat.lua_51 = False
            compat.reasons.append(f"Lua 5.2+: {reason}")
    
    # Check for features incompatible with Lua 5.1
    for pattern, reason in LUA_51_INCOMPATIBLE:
        if re.search(pattern, lua_code):
            if compat.lua_51:
                compat.lua_51 = False
                compat.reasons.append(f"Not Lua 5.1: {reason}")
    
    # Check for Lua 5.1-only features (deprecated in 5.2+)
    # When these are used, code is 5.1-only and won't work in any later version
    for pattern, reason in LUA_51_ONLY_FEATURES:
        if re.search(pattern, lua_code):
            compat.lua_52 = False
            compat.lua_53 = False
            compat.lua_54 = False
            compat.lua_55 = False
            compat.reasons.append(f"Lua 5.1 only: {reason}")

    # Check for Lua 5.2-only features (not in 5.1, removed in 5.3+)
    # bit32 is the primary example: added in 5.2, deprecated and removed in 5.3+
    for pattern, reason in LUA_52_ONLY_FEATURES:
        if re.search(pattern, lua_code):
            compat.lua_51 = False
            compat.lua_53 = False
            compat.lua_54 = False
            compat.lua_55 = False
            compat.reasons.append(f"Lua 5.2 only: {reason}")

    # Check for Lua 5.5 specific features (forward compatibility)
    for pattern, reason in LUA_55_FEATURES:
        if re.search(pattern, lua_code):
            compat.lua_51 = False
            compat.lua_52 = False
            compat.lua_53 = False
            compat.lua_54 = False
            compat.reasons.append(f"Lua 5.5+: {reason}")
    
    # Check if test uses undefined globals (likely interop tests)
    # Common interop variable names - variables typically injected by C# test code
    # Extended list based on common patterns found in the test codebase
    interop_vars = [
        'o1', 'o2', 'o3', 'o4', 'o5',  # Generic object placeholders
        'obj', 'myobj', 'testObj',     # Object references
        'instance', 'static',          # Instance/static references
        'userdata',                    # UserData wrapper
        'arr', 'array',                # Array types
        'list', 'dict', 'map',         # Collection types
        'callback', 'func',            # Function references
        'cls', 'clsInstance',          # Class references
        'vec', 'v3',                   # Vector types (Unity common)
        'stream', 'file',              # IO types
        'sb', 'builder',               # StringBuilder types
        's', 'r',                      # Short variable names from tests
        'throw_reader_helper',         # LoadModule test helpers
        'reader_helper',               # LoadModule test helpers
    ]
    for var in interop_vars:
        if re.search(rf'\b{var}\b', lua_code) and f'{var} =' not in lua_code and f'local {var}' not in lua_code:
            # Variable used but not defined - likely injected by C# test
            compat.novasharp_only = True
            compat.reasons.append(f"Uses injected variable: {var}")
            break
    
    return compat


def check_expects_error(surrounding_context: str) -> bool:
    """Check if the test expects an error from the Lua code."""
    for pattern in ERROR_EXPECTING_PATTERNS:
        if re.search(pattern, surrounding_context):
            return True
    return False


def extract_snippets_from_file(file_path: Path) -> Iterator[LuaSnippet]:
    """Extract all Lua snippets from a C# test file."""
    try:
        content = file_path.read_text(encoding='utf-8')
    except Exception as e:
        print(f"Warning: Could not read {file_path}: {e}", file=sys.stderr)
        return
    
    # Build variable lookup table for resolving variable references
    var_lookup = build_variable_lookup(content)
    
    method_snippet_counts: dict[str, int] = {}
    
    for match in DOSTRING_CALL_PATTERN.finditer(content):
        lua_code, is_variable = extract_lua_from_match(match)
        position = match.start()
        
        # Try to resolve variable references using position-aware lookup
        if is_variable:
            resolved = resolve_variable(lua_code, position, var_lookup)
            if resolved is not None:
                lua_code = resolved
                is_variable = False
        
        if is_variable:
            continue
        
        if not lua_code.strip():
            continue
        
        line_number = count_lines_before(content, position)
        test_class = find_containing_class(content, position)
        test_method = find_containing_method(content, position)
        
        # Get surrounding content for context analysis (larger window)
        start_ctx = max(0, position - 1000)
        end_ctx = min(len(content), position + len(lua_code) + 500)
        surrounding = content[start_ctx:end_ctx]
        
        compatibility = analyze_version_compatibility(lua_code, surrounding, test_class, test_method)
        expects_error = check_expects_error(surrounding)
        
        key = f"{test_class}.{test_method}"
        snippet_index = method_snippet_counts.get(key, 0)
        method_snippet_counts[key] = snippet_index + 1
        
        yield LuaSnippet(
            lua_code=lua_code.strip(),
            # POSIX separators unconditionally: `str(PurePath)` is OS-native, so
            # regenerating on Linux rewrote every fixture written on Windows and
            # vice versa, burying real metadata changes in thousands of
            # separator-only diffs.
            source_file=file_path.relative_to(ROOT).as_posix(),
            line_number=line_number,
            test_class=test_class,
            test_method=test_method,
            compatibility=compatibility,
            expects_error=expects_error,
            snippet_index=snippet_index,
        )


def discover_test_files(test_dirs: list[Path]) -> Iterator[Path]:
    """Discover all C# test files in the given directories."""
    for test_dir in test_dirs:
        if not test_dir.exists():
            continue
        for cs_file in sorted(test_dir.rglob("*.cs")):
            if any(skip in cs_file.name for skip in ["AssemblyInfo", ".g.cs", "GlobalUsings", "_Hardwired"]):
                continue
            yield cs_file


def extract_all_snippets(test_dirs: list[Path]) -> ExtractionResult:
    """Extract all Lua snippets from test files."""
    result = ExtractionResult()
    
    for cs_file in discover_test_files(test_dirs):
        try:
            snippets = list(extract_snippets_from_file(cs_file))
            result.snippets.extend(snippets)
        except Exception as e:
            result.errors.append(f"{cs_file}: {e}")
    
    return result


def _existing_fixture_indexes(
    output_dir: Path, test_class: str, test_method: str
) -> dict[int, tuple[list[str], str, str | None]]:
    """Return existing slots as ``index -> (header, body, source file)``."""
    fixture_dir = output_dir / test_class
    if not fixture_dir.is_dir():
        return {}

    filename_pattern = re.compile(rf"^{re.escape(test_method)}(?:_([0-9]+))?\.lua$")
    existing: dict[int, tuple[list[str], str, str | None]] = {}
    for fixture in sorted(fixture_dir.glob(f"{test_method}*.lua")):
        match = filename_pattern.fullmatch(fixture.name)
        if match is None:
            continue

        index = int(match.group(1) or 0)
        header, body = split_fixture_header(fixture.read_text(encoding="utf-8"))
        source = parse_header_metadata(header).get("@source")
        source_file = source.rsplit(":", 1)[0] if source and ":" in source else None
        existing[index] = (header, body, source_file)

    return existing


def reconcile_snippet_output_paths(result: ExtractionResult, output_dir: Path) -> None:
    """Make generated paths unique and keep curated metadata with its Lua body.

    Test classes with the same short name can live in different namespaces. The
    per-file snippet counter previously assigned those tests the same output path,
    so later writes silently replaced earlier Lua programs and the manifest held
    duplicate entries. Identical programs need one comparison; distinct programs
    receive separate numeric slots. Stable source ordering owns slot assignment;
    an existing fixture contributes curated metadata only to the matching Lua
    body, so renumbering cannot attach compatibility decisions to another program.
    """
    groups: dict[tuple[str, str], list[LuaSnippet]] = {}
    for snippet in result.snippets:
        groups.setdefault((snippet.test_class, snippet.test_method), []).append(snippet)

    reconciled: list[LuaSnippet] = []
    for (test_class, test_method), snippets in groups.items():
        by_slot_and_code: dict[tuple[int, str], list[LuaSnippet]] = {}
        for snippet in snippets:
            by_slot_and_code.setdefault(
                (snippet.snippet_index, snippet.lua_code), []
            ).append(snippet)

        existing = _existing_fixture_indexes(output_dir, test_class, test_method)
        ordered_keys = sorted(
            by_slot_and_code,
            key=lambda key: (
                key[0],
                min(
                    (snippet.source_file, snippet.line_number)
                    for snippet in by_slot_and_code[key]
                ),
            ),
        )
        used_indexes: set[int] = set()
        for original_index, code in ordered_keys:
            candidates = by_slot_and_code[(original_index, code)]
            matching_existing = [
                (index, header, source_file)
                for index, (header, body, source_file) in existing.items()
                if index not in used_indexes and fixture_body_matches(header, body, code)
            ]
            matching_sources = {
                source_file for _, _, source_file in matching_existing if source_file
            }
            representative = min(
                candidates,
                key=lambda snippet: (
                    snippet.source_file not in matching_sources,
                    snippet.source_file,
                    snippet.line_number,
                ),
            )

            preferred_existing = sorted(
                matching_existing,
                key=lambda item: (
                    item[2] != representative.source_file,
                    item[0] != original_index,
                    item[0],
                ),
            )
            if preferred_existing:
                target_index, header, _ = preferred_existing[0]
                representative.curated_header_lines = strip_absorbed_body_prefix(
                    header, code
                )
            else:
                target_index = original_index
                if target_index in existing or target_index in used_indexes:
                    target_index = 0
                    while target_index in existing or target_index in used_indexes:
                        target_index += 1

            representative.snippet_index = target_index
            used_indexes.add(target_index)
            reconciled.append(representative)

    result.snippets = reconciled


def find_orphaned_fixture_paths(
    result: ExtractionResult, output_dir: Path
) -> list[str]:
    """Return fixture paths that no current extracted snippet owns.

    Orphans are intentionally reported rather than removed. Many are curated,
    standalone comparison programs that have no one-to-one C# source snippet.
    """
    if not output_dir.is_dir():
        return []

    generated = {snippet.output_path for snippet in result.snippets}
    fixtures = {
        path.relative_to(output_dir).as_posix()
        for path in output_dir.rglob("*.lua")
    }
    return sorted(fixtures - generated)


def write_snippets(result: ExtractionResult, output_dir: Path, dry_run: bool = False) -> None:
    """Write extracted snippets to the output directory."""
    if dry_run:
        print(f"[DRY RUN] Would create {result.total_snippets} files in {output_dir}")
        return
    
    output_dir.mkdir(parents=True, exist_ok=True)
    
    for snippet in result.snippets:
        snippet_dir = output_dir / snippet.test_class
        snippet_dir.mkdir(parents=True, exist_ok=True)
        
        snippet_path = snippet_dir / snippet.output_filename
        content = snippet.generate_header() + snippet.lua_code + "\n"
        snippet_path.write_text(content, encoding='utf-8')


def write_manifest(result: ExtractionResult, output_dir: Path, dry_run: bool = False) -> None:
    """Write the manifest file with snippet metadata."""
    snippets = _order_snippets_for_manifest(result.snippets, output_dir)
    manifest = {
        "generated_by": "tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py",
        "total_snippets": result.total_snippets,
        "novasharp_only": result.novasharp_only_count,
        "comparable": result.comparable_count,
        "by_version": {
            "5.1": len(result.by_version("5.1")),
            "5.2": len(result.by_version("5.2")),
            "5.3": len(result.by_version("5.3")),
            "5.4": len(result.by_version("5.4")),
            "5.5": len(result.by_version("5.5")),
        },
        "snippets": [
            {
                "path": snippet.output_path,
                "source": snippet.emitted_header_values["@source"],
                "test": snippet.emitted_header_values["@test"],
                "lua_versions": snippet.compatibility.compatible_versions,
                "novasharp_only": snippet.compatibility.novasharp_only,
                "expects_error": snippet.expects_error,
            }
            for snippet in snippets
        ]
    }
    
    if dry_run:
        print(f"[DRY RUN] Would write manifest with {result.total_snippets} entries")
        return
    
    manifest_path = output_dir / "manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding='utf-8')


def _order_snippets_for_manifest(
    snippets: list[LuaSnippet], output_dir: Path
) -> list[LuaSnippet]:
    """Preserve existing manifest order and append newly generated paths.

    Extraction order is deterministic but occasionally changes when tests move
    between files. Keeping the established path order prevents unrelated source
    movement from rewriting the entire manifest.
    """
    by_path = {snippet.output_path: snippet for snippet in snippets}
    ordered: list[LuaSnippet] = []
    seen: set[str] = set()
    manifest_path = output_dir / "manifest.json"

    try:
        existing = json.loads(manifest_path.read_text(encoding="utf-8"))
        existing_paths = [entry["path"] for entry in existing["snippets"]]
    except (FileNotFoundError, KeyError, TypeError, json.JSONDecodeError, OSError):
        existing_paths = []

    for path in existing_paths:
        if path in by_path and path not in seen:
            ordered.append(by_path[path])
            seen.add(path)

    ordered.extend(
        by_path[path] for path in sorted(by_path.keys() - seen)
    )
    return ordered


def print_orphan_summary(
    result: ExtractionResult, output_dir: Path, report_all: bool = False
) -> None:
    """Report fixtures that require human triage without deleting them."""
    orphaned = find_orphaned_fixture_paths(result, output_dir)
    print("\n=== Orphaned Fixtures ===")
    if not orphaned:
        print("No fixture paths are unowned by extracted source snippets.")
        return

    shown = orphaned if report_all else orphaned[:20]
    print(
        f"Found {len(orphaned)} fixture path(s) with no extracted source owner; "
        "none were deleted."
    )
    for path in shown:
        print(f"  {path}")
    if len(shown) < len(orphaned):
        print(
            f"  ... {len(orphaned) - len(shown)} more "
            "(pass --report-orphans to list all)"
        )


def print_curation_summary(
    result: ExtractionResult, overrides: list[CuratedOverride]
) -> None:
    """Report how many fixtures kept curated metadata, and where it diverged."""
    preserved = sum(1 for s in result.snippets if s.curated_header_lines is not None)
    new_fixtures = result.total_snippets - preserved

    print(f"\n=== Curated Metadata ===")
    print(f"Existing fixtures (header preserved): {preserved}")
    print(f"New fixtures (metadata computed):     {new_fixtures}")

    if not overrides:
        print("Curated headers agree with the recomputed metadata.")
        return

    by_key: dict[str, list[CuratedOverride]] = {}
    for override in overrides:
        by_key.setdefault(override.key, []).append(override)

    print(
        f"Kept {len(overrides)} curated value(s) that the heuristics would have changed:"
    )
    for key in CURATED_KEYS:
        entries = by_key.get(key, [])
        if not entries:
            continue
        print(f"  {key}: {len(entries)}")
        for entry in entries[:5]:
            print(f"    {entry.path}: kept {entry.curated!r} (computed {entry.computed!r})")
        if len(entries) > 5:
            print(f"    ... and {len(entries) - 5} more")


def print_summary(result: ExtractionResult) -> None:
    """Print extraction summary."""
    print(f"\n=== Lua Corpus Extraction Summary ===")
    print(f"Total snippets:     {result.total_snippets}")
    print(f"NovaSharp-only:     {result.novasharp_only_count}")
    print(f"Comparable:         {result.comparable_count}")
    print(f"\nBy Lua version:")
    print(f"  Lua 5.1: {len(result.by_version('5.1'))}")
    print(f"  Lua 5.2: {len(result.by_version('5.2'))}")
    print(f"  Lua 5.3: {len(result.by_version('5.3'))}")
    print(f"  Lua 5.4: {len(result.by_version('5.4'))}")
    print(f"  Lua 5.5: {len(result.by_version('5.5'))}")
    
    if result.errors:
        print(f"\nErrors: {len(result.errors)}")
        for err in result.errors[:5]:
            print(f"  - {err}")


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Extract Lua snippets from NovaSharp C# tests with version metadata"
    )
    parser.add_argument(
        '--output-dir',
        type=Path,
        default=DEFAULT_OUTPUT_DIR,
        help=f'Output directory (default: {DEFAULT_OUTPUT_DIR.relative_to(ROOT)})'
    )
    parser.add_argument(
        '--dry-run',
        action='store_true',
        help="Don't write files, just show what would be extracted"
    )
    parser.add_argument(
        '--manifest-only',
        action='store_true',
        help="Only write the manifest file, not individual Lua files"
    )
    parser.add_argument(
        '--refresh-metadata',
        action='store_true',
        help=(
            "Recompute @lua-versions / @novasharp-only / @expects-error for fixtures "
            "that already exist, discarding curated values. Every changed fixture must "
            "be re-audited against reference Lua before committing."
        )
    )
    parser.add_argument(
        '--report-orphans',
        action='store_true',
        help="List every existing fixture path with no extracted source owner",
    )

    args = parser.parse_args()

    print(f"Extracting Lua snippets from test files...")
    result = extract_all_snippets(DEFAULT_TEST_DIRS)
    reconcile_snippet_output_paths(result, args.output_dir)

    if args.refresh_metadata:
        print(
            "\n[--refresh-metadata] Curated fixture metadata will be overwritten; "
            "re-audit every changed fixture against reference Lua."
        )
    else:
        overrides = apply_curated_metadata(result, args.output_dir)
        print_curation_summary(result, overrides)

    print_orphan_summary(result, args.output_dir, report_all=args.report_orphans)
    print_summary(result)

    if not args.manifest_only:
        write_snippets(result, args.output_dir, dry_run=args.dry_run)
    
    write_manifest(result, args.output_dir, dry_run=args.dry_run)
    
    if not args.dry_run:
        print(f"\nOutput written to: {args.output_dir}")
    
    return 0


if __name__ == '__main__':
    sys.exit(main())
