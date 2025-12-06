#!/usr/bin/env python3
"""Extract Lua snippets from NovaSharp C# test files with version compatibility metadata.

This tool parses C# test files looking for `DoString(...)` calls and extracts
the Lua code from string literals. The extracted snippets are written to
`src/tests/NovaSharp.Interpreter.Tests/LuaFixtures/` with version compatibility
headers so they can be tested against real Lua runtimes.

Each extracted file includes a metadata header:
    -- @lua-versions: 5.1, 5.2, 5.3, 5.4
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
DEFAULT_OUTPUT_DIR = ROOT / "src" / "tests" / "NovaSharp.Interpreter.Tests" / "LuaFixtures"
DEFAULT_TEST_DIRS = [
    ROOT / "src" / "tests" / "NovaSharp.Interpreter.Tests.TUnit",
    ROOT / "src" / "tests" / "NovaSharp.Interpreter.Tests",
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

# Pattern to match test method declarations
TEST_METHOD_PATTERN = re.compile(
    r'\[(?:global::)?TUnit\.Core\.Test\].*?'
    r'(?:public\s+)?(?:async\s+)?(?:Task|void)\s+(\w+)\s*\(',
    re.DOTALL
)

# Pattern to match test class declarations
TEST_CLASS_PATTERN = re.compile(
    r'(?:public\s+)?(?:sealed\s+)?class\s+(\w+)'
)

# Lua version feature detection patterns
LUA_54_FEATURES = [
    (r'<const>', 'const attribute'),
    (r'<close>', 'close attribute'),
    (r'\bwarn\s*\(', 'warn function'),
    (r'goto\s+\w+', 'goto statement'),
    (r'::\w+::', 'label'),
]

LUA_53_FEATURES = [
    (r'\b//\b', 'floor division'),
    (r'[^~]=', 'bitwise operators'),
    (r'&(?![&])', 'bitwise AND'),
    (r'\|(?!\|)', 'bitwise OR'),
    (r'~(?!=)', 'bitwise XOR/NOT'),
    (r'<<|>>', 'bit shift'),
    (r'utf8\.', 'utf8 library'),
    (r'table\.move\s*\(', 'table.move'),
]

LUA_52_FEATURES = [
    (r'bit32\.', 'bit32 library'),
    (r'_ENV\b', '_ENV variable'),
    (r'package\.searchpath', 'package.searchpath'),
    (r'rawlen\s*\(', 'rawlen function'),
]

LUA_51_INCOMPATIBLE = [
    # Features that don't work in Lua 5.1
    (r'\b//\b', 'floor division'),
    (r'&(?![&])', 'bitwise AND'),
    (r'\|(?!\|)', 'bitwise OR'),
    (r'goto\s+\w+', 'goto'),
    (r'<const>', 'const attribute'),
    (r'<close>', 'close attribute'),
]

NOVASHARP_SPECIFIC_PATTERNS = [
    (r'\b!=\b', 'C-style not-equal'),
    (r'_NOVASHARP', 'NovaSharp global'),
    (r'clr\.', 'CLR interop'),
    (r'import\s*\(', 'NovaSharp import'),
    (r'dynamic\.', 'dynamic access'),
    (r'using\s+', 'using statement (non-Lua)'),
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
        return versions
    
    @property
    def version_string(self) -> str:
        """Return comma-separated version string."""
        if self.novasharp_only:
            return "novasharp-only"
        versions = self.compatible_versions
        if not versions:
            return "none"
        if len(versions) == 4:
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


def analyze_version_compatibility(lua_code: str, surrounding_context: str) -> LuaVersionCompatibility:
    """Analyze Lua code to determine version compatibility."""
    compat = LuaVersionCompatibility()
    
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
    
    # If NovaSharp-only, skip further analysis
    if compat.novasharp_only:
        return compat
    
    # Check for Lua 5.4 specific features
    for pattern, reason in LUA_54_FEATURES:
        if re.search(pattern, lua_code):
            compat.lua_51 = False
            compat.lua_52 = False
            compat.lua_53 = False
            compat.reasons.append(f"Lua 5.4: {reason}")
    
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
    
    # Check if test uses undefined globals (likely interop tests)
    # Common interop variable names
    interop_vars = ['o1', 'o2', 'obj', 'myobj', 'instance', 'static', 'testObj', 'userdata']
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
    
    method_snippet_counts: dict[str, int] = {}
    
    for match in DOSTRING_CALL_PATTERN.finditer(content):
        lua_code, is_variable = extract_lua_from_match(match)
        
        if is_variable:
            continue
        
        if not lua_code.strip():
            continue
        
        position = match.start()
        line_number = count_lines_before(content, position)
        test_class = find_containing_class(content, position)
        test_method = find_containing_method(content, position)
        
        # Get surrounding content for context analysis (larger window)
        start_ctx = max(0, position - 1000)
        end_ctx = min(len(content), position + len(lua_code) + 500)
        surrounding = content[start_ctx:end_ctx]
        
        compatibility = analyze_version_compatibility(lua_code, surrounding)
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
