#!/usr/bin/env python3
"""Extract Lua snippets from NovaSharp C# test files for specification comparison.

This tool parses C# test files looking for `DoString(...)` calls and extracts
the Lua code from string literals. The extracted snippets are written to
`artifacts/lua-corpus/<TestClass>/<TestMethod>.lua` with a manifest file
for tracking and automation.

Usage:
    python tools/LuaCorpusExtractor/lua_corpus_extractor.py
    python tools/LuaCorpusExtractor/lua_corpus_extractor.py --output-dir custom/path
    python tools/LuaCorpusExtractor/lua_corpus_extractor.py --manifest-only
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
DEFAULT_OUTPUT_DIR = ROOT / "artifacts" / "lua-corpus"
DEFAULT_TEST_DIRS = [
    ROOT / "src" / "tests" / "NovaSharp.Interpreter.Tests.TUnit",
    ROOT / "src" / "tests" / "NovaSharp.Interpreter.Tests",
]

# Pattern to match DoString calls with various string literal forms
# Handles: DoString("..."), DoString(@"..."), DoString($"..."), DoString($@"...")
# Also handles multi-line strings and raw string literals (C# 11+)
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

# Comment marker for NovaSharp-specific tests that should skip Lua 5.4 comparison
SKIP_COMPARISON_MARKER = "novasharp: skip-comparison"


@dataclass
class LuaSnippet:
    """Represents an extracted Lua snippet with metadata."""
    
    lua_code: str
    source_file: str
    line_number: int
    test_class: str
    test_method: str
    is_novasharp_specific: bool = False
    snippet_index: int = 0  # For multiple snippets in same method
    
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


@dataclass
class ExtractionResult:
    """Result of extracting snippets from all test files."""
    
    snippets: list[LuaSnippet] = field(default_factory=list)
    errors: list[str] = field(default_factory=list)
    skipped_files: list[str] = field(default_factory=list)
    
    @property
    def total_snippets(self) -> int:
        return len(self.snippets)
    
    @property
    def novasharp_specific_count(self) -> int:
        return sum(1 for s in self.snippets if s.is_novasharp_specific)
    
    @property
    def comparable_count(self) -> int:
        return sum(1 for s in self.snippets if not s.is_novasharp_specific)


def unescape_csharp_string(content: str, is_verbatim: bool = False) -> str:
    """Convert C# string literal escapes to actual characters."""
    if is_verbatim:
        # In verbatim strings, "" becomes " and that's the only escape
        return content.replace('""', '"')
    
    # Regular string escapes
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
        # Interpolated strings might contain C# expressions - mark for review
        return unescape_csharp_string(match.group('interp_verbatim'), is_verbatim=True), False
    if match.group('interp') is not None:
        return unescape_csharp_string(match.group('interp'), is_verbatim=False), False
    if match.group('variable') is not None:
        # Variable reference - can't extract statically
        return match.group('variable'), True
    return "", True


def find_containing_class(content: str, position: int) -> str:
    """Find the class name containing the given position."""
    # Look backwards from position to find the most recent class declaration
    search_content = content[:position]
    matches = list(TEST_CLASS_PATTERN.finditer(search_content))
    if matches:
        return matches[-1].group(1)
    return "Unknown"


def find_containing_method(content: str, position: int) -> str:
    """Find the test method name containing the given position."""
    # Look backwards from position to find the most recent test method
    search_content = content[:position]
    matches = list(TEST_METHOD_PATTERN.finditer(search_content))
    if matches:
        return matches[-1].group(1)
    return "Unknown"


def count_lines_before(content: str, position: int) -> int:
    """Count the number of newlines before the given position."""
    return content[:position].count('\n') + 1


def check_novasharp_specific(lua_code: str, surrounding_content: str) -> bool:
    """Check if the Lua snippet uses NovaSharp-specific features."""
    # Check for explicit skip marker
    if SKIP_COMPARISON_MARKER in surrounding_content:
        return True
    
    # Check for NovaSharp-specific patterns in the Lua code
    novasharp_patterns = [
        r'_NOVASHARP',  # NovaSharp-specific global
        r'clr\.',       # CLR interop
        r'import\s*\(', # Module import (NovaSharp extension)
    ]
    
    for pattern in novasharp_patterns:
        if re.search(pattern, lua_code, re.IGNORECASE):
            return True
    
    return False


def extract_snippets_from_file(file_path: Path) -> Iterator[LuaSnippet]:
    """Extract all Lua snippets from a C# test file."""
    try:
        content = file_path.read_text(encoding='utf-8')
    except Exception as e:
        print(f"Warning: Could not read {file_path}: {e}", file=sys.stderr)
        return
    
    # Track snippets per method for indexing
    method_snippet_counts: dict[str, int] = {}
    
    for match in DOSTRING_CALL_PATTERN.finditer(content):
        lua_code, is_variable = extract_lua_from_match(match)
        
        if is_variable:
            # Can't extract variable references statically
            continue
        
        if not lua_code.strip():
            continue
        
        position = match.start()
        line_number = count_lines_before(content, position)
        test_class = find_containing_class(content, position)
        test_method = find_containing_method(content, position)
        
        # Get surrounding content for context analysis
        start_ctx = max(0, position - 500)
        end_ctx = min(len(content), position + len(lua_code) + 200)
        surrounding = content[start_ctx:end_ctx]
        
        is_novasharp_specific = check_novasharp_specific(lua_code, surrounding)
        
        # Track snippet index for multiple snippets in same method
        key = f"{test_class}.{test_method}"
        snippet_index = method_snippet_counts.get(key, 0)
        method_snippet_counts[key] = snippet_index + 1
        
        yield LuaSnippet(
            lua_code=lua_code.strip(),
            source_file=str(file_path.relative_to(ROOT)),
            line_number=line_number,
            test_class=test_class,
            test_method=test_method,
            is_novasharp_specific=is_novasharp_specific,
            snippet_index=snippet_index,
        )


def discover_test_files(test_dirs: list[Path]) -> Iterator[Path]:
    """Discover all C# test files in the given directories."""
    for test_dir in test_dirs:
        if not test_dir.exists():
            continue
        for cs_file in test_dir.rglob("*.cs"):
            # Skip generated/infrastructure files
            if any(skip in cs_file.name for skip in ["AssemblyInfo", ".g.cs", "GlobalUsings"]):
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


def write_snippets(result: ExtractionResult, output_dir: Path) -> None:
    """Write extracted snippets to the output directory."""
    output_dir.mkdir(parents=True, exist_ok=True)
    
    for snippet in result.snippets:
        snippet_dir = output_dir / snippet.test_class
        snippet_dir.mkdir(parents=True, exist_ok=True)
        
        snippet_path = snippet_dir / snippet.output_filename
        
        # Add header comment with metadata
        header_lines = [
            f"-- Source: {snippet.source_file}:{snippet.line_number}",
            f"-- Test: {snippet.test_class}.{snippet.test_method}",
        ]
        if snippet.is_novasharp_specific:
            header_lines.append(f"-- {SKIP_COMPARISON_MARKER}")
        header_lines.append("")
        
        content = "\n".join(header_lines) + snippet.lua_code + "\n"
        snippet_path.write_text(content, encoding='utf-8')


def write_manifest(result: ExtractionResult, output_dir: Path) -> None:
    """Write the manifest file with snippet metadata."""
    manifest = {
        "generated": True,
        "total_snippets": result.total_snippets,
        "comparable_snippets": result.comparable_count,
        "novasharp_specific_snippets": result.novasharp_specific_count,
        "errors": result.errors,
        "snippets": [
            {
                "path": snippet.output_path,
                "source_file": snippet.source_file,
                "line_number": snippet.line_number,
                "test_class": snippet.test_class,
                "test_method": snippet.test_method,
                "is_novasharp_specific": snippet.is_novasharp_specific,
            }
            for snippet in result.snippets
        ]
    }
    
    manifest_path = output_dir / "manifest.json"
    manifest_path.write_text(json.dumps(manifest, indent=2) + "\n", encoding='utf-8')


def print_summary(result: ExtractionResult) -> None:
    """Print extraction summary to stdout."""
    print(f"Lua Corpus Extraction Summary")
    print(f"=============================")
    print(f"Total snippets extracted: {result.total_snippets}")
    print(f"  Comparable (pure Lua): {result.comparable_count}")
    print(f"  NovaSharp-specific:    {result.novasharp_specific_count}")
    
    if result.errors:
        print(f"\nErrors ({len(result.errors)}):")
        for error in result.errors:
            print(f"  - {error}")
    
    if result.skipped_files:
        print(f"\nSkipped files ({len(result.skipped_files)}):")
        for skipped in result.skipped_files[:10]:
            print(f"  - {skipped}")
        if len(result.skipped_files) > 10:
            print(f"  ... and {len(result.skipped_files) - 10} more")


def parse_args() -> argparse.Namespace:
    """Parse command-line arguments."""
    parser = argparse.ArgumentParser(
        description="Extract Lua snippets from NovaSharp C# test files"
    )
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=DEFAULT_OUTPUT_DIR,
        help=f"Output directory for extracted snippets (default: {DEFAULT_OUTPUT_DIR})",
    )
    parser.add_argument(
        "--manifest-only",
        action="store_true",
        help="Only generate the manifest, don't write snippet files",
    )
    parser.add_argument(
        "--quiet",
        action="store_true",
        help="Suppress summary output",
    )
    parser.add_argument(
        "--test-dirs",
        type=Path,
        nargs="+",
        default=DEFAULT_TEST_DIRS,
        help="Directories to search for test files",
    )
    return parser.parse_args()


def main() -> int:
    """Main entry point."""
    args = parse_args()
    
    result = extract_all_snippets(args.test_dirs)
    
    if not args.manifest_only:
        write_snippets(result, args.output_dir)
    
    write_manifest(result, args.output_dir)
    
    if not args.quiet:
        print_summary(result)
        print(f"\nOutput written to: {args.output_dir}")
    
    return 0 if not result.errors else 1


if __name__ == "__main__":
    sys.exit(main())
