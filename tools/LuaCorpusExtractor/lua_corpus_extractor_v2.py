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

Usage:
    python tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py
    python tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py --dry-run
    python tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py --output-dir custom/path
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
    (r'bit32\.', 'bit32 library'),
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
    (r'string\.contains\s*\(', 'NovaSharp string extension'),
    (r'string\.unicode\s*\(', 'NovaSharp string extension'),
    (r'Script\.GlobalOptions', 'NovaSharp Script.GlobalOptions'),
    (r'sandbox', 'potential NovaSharp sandbox'),
    (r'debug\.debug\s*\(\s*\)', 'debug.debug() is interactive/platform-dependent'),
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
        """Return list of compatible Lua versions."""
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
    
    def generate_header(self) -> str:
        """Generate the metadata header for the Lua file."""
        lines = [
            f"-- @lua-versions: {self.compatibility.version_string}",
            f"-- @novasharp-only: {str(self.compatibility.novasharp_only).lower()}",
            f"-- @expects-error: {str(self.expects_error).lower()}",
            f"-- @source: {self.source_file}:{self.line_number}",
            f"-- @test: {self.test_class}.{self.test_method}",
        ]
        if self.compatibility.reasons:
            lines.append(f"-- @compat-notes: {'; '.join(self.compatibility.reasons)}")
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
]


def analyze_version_compatibility(lua_code: str, surrounding_context: str, test_class: str = "") -> LuaVersionCompatibility:
    """Analyze Lua code to determine version compatibility."""
    compat = LuaVersionCompatibility()
    
    # Check if test class indicates NovaSharp-only functionality
    for prefix in NOVASHARP_ONLY_TEST_CLASS_PREFIXES:
        if test_class.startswith(prefix):
            compat.novasharp_only = True
            compat.reasons.append(f"Test class '{test_class}' uses NovaSharp-specific {prefix} functionality")
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
        
        compatibility = analyze_version_compatibility(lua_code, surrounding, test_class)
        expects_error = check_expects_error(surrounding)
        
        key = f"{test_class}.{test_method}"
        snippet_index = method_snippet_counts.get(key, 0)
        method_snippet_counts[key] = snippet_index + 1
        
        yield LuaSnippet(
            lua_code=lua_code.strip(),
            source_file=str(file_path.relative_to(ROOT)),
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
        for cs_file in test_dir.rglob("*.cs"):
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
                "source": f"{snippet.source_file}:{snippet.line_number}",
                "test": f"{snippet.test_class}.{snippet.test_method}",
                "lua_versions": snippet.compatibility.compatible_versions,
                "novasharp_only": snippet.compatibility.novasharp_only,
                "expects_error": snippet.expects_error,
            }
            for snippet in result.snippets
        ]
    }
    
    if dry_run:
        print(f"[DRY RUN] Would write manifest with {result.total_snippets} entries")
        return
    
    manifest_path = output_dir / "manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2), encoding='utf-8')


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
    
    args = parser.parse_args()
    
    print(f"Extracting Lua snippets from test files...")
    result = extract_all_snippets(DEFAULT_TEST_DIRS)
    
    print_summary(result)
    
    if not args.manifest_only:
        write_snippets(result, args.output_dir, dry_run=args.dry_run)
    
    write_manifest(result, args.output_dir, dry_run=args.dry_run)
    
    if not args.dry_run:
        print(f"\nOutput written to: {args.output_dir}")
    
    return 0


if __name__ == '__main__':
    sys.exit(main())
