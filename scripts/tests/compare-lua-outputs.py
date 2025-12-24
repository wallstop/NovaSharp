#!/usr/bin/env python3
"""
compare-lua-outputs.py - Compare Lua execution outputs with semantic normalization

This script compares outputs from reference Lua and NovaSharp executions,
applying semantic normalization to handle expected differences:
- Floating-point precision differences
- Memory address variations in error messages
- Line number differences in stack traces
- Platform-specific path separators

Usage:
    python3 scripts/tests/compare-lua-outputs.py [OPTIONS]

Options:
    --results-dir DIR       Directory with execution results (default: artifacts/lua-corpus-results)
    --output-file FILE      Output comparison report (default: artifacts/lua-corpus-results/comparison.json)
    --lua-version VER       Lua version to compare against (default: 5.4)
    --allowlist FILE        JSON file with known divergences to exclude from failure (default: none)
    --verbose               Show detailed differences
    --strict                Don't apply semantic normalization
    --enforce               Exit with error on unexpected mismatches (for CI gating)
"""

import argparse
import json
import os
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional


# Default fixture directory for version compatibility checks
DEFAULT_FIXTURES_DIR = Path(__file__).resolve().parents[2] / "src" / "tests" / "WallstopStudios.NovaSharp.Interpreter.Tests" / "LuaFixtures"


def parse_fixture_version_info(fixture_path: Path) -> tuple[list[str], bool]:
    """
    Parse version metadata from a fixture file header.
    Returns (lua_versions, novasharp_only).
    """
    lua_versions = []
    novasharp_only = False
    
    try:
        with open(fixture_path, 'r', encoding='utf-8') as f:
            for _ in range(10):
                line = f.readline()
                if not line.startswith("--"):
                    break
                
                if "@lua-versions:" in line:
                    versions_part = line.split("@lua-versions:")[1].strip()
                    if "novasharp-only" in versions_part.lower():
                        novasharp_only = True
                    else:
                        lua_versions = [v.strip() for v in versions_part.split(",")]
                
                if "@novasharp-only: true" in line.lower():
                    novasharp_only = True
    except Exception:
        pass
    
    return lua_versions, novasharp_only


def is_fixture_compatible(lua_versions: list[str], target_version: str, novasharp_only: bool) -> bool:
    """Check if a fixture is compatible with the given Lua version."""
    if novasharp_only:
        return False
    
    if not lua_versions:
        return True  # No version info, assume compatible
    
    # Check for exact match
    if target_version in lua_versions:
        return True
    
    # Check for "5.3+" style patterns
    try:
        target_num = int(target_version.replace(".", ""))
        for v in lua_versions:
            if v.endswith("+"):
                base_version = int(v.rstrip("+").replace(".", ""))
                if target_num >= base_version:
                    return True
    except ValueError:
        pass
    
    return False


# Known divergences that don't represent bugs in NovaSharp.
# Format: list of fixture paths (relative to corpus dir) that should be excluded from failure.
# See docs/testing/lua-divergences.md for documentation of each divergence.
#
# NOTE (2025-12-20 - Session 051): This set was audited and found to be largely obsolete.
# Most entries are now skipped via @novasharp-only fixture metadata or handled via version
# compatibility filtering. The set is kept minimal for potential edge cases only.
# Audit result: 0 unexpected mismatches across all Lua versions (5.1, 5.2, 5.3, 5.4, 5.5).
KNOWN_DIVERGENCES = {
    # Reserved for future entries if needed.
    # All previously listed divergences are now:
    # - Skipped via @novasharp-only: true in fixture headers (CLR interop fixtures)
    # - Handled via @lua-versions metadata (version-specific features)
    # - Classified as both_error (both interpreters reject invalid code)
}


@dataclass
class ComparisonResult:
    """Result of comparing a single snippet's outputs."""
    file: str
    lua_version: str
    status: str  # "match", "mismatch", "lua_only", "nova_only", "both_error", "skipped", "known_divergence"
    lua_output: str = ""
    nova_output: str = ""
    lua_error: str = ""
    nova_error: str = ""
    lua_rc: int = 0
    nova_rc: int = 0
    normalized_match: bool = False
    diff_summary: str = ""


def normalize_output(text: str, strict: bool = False) -> str:
    """
    Apply semantic normalization to Lua output.
    
    Normalizations applied:
    - NovaSharp CLI: Remove [compatibility] info lines
    - Floating-point: Round to 10 decimal places, normalize -0 to 0
    - Memory addresses: Replace hex addresses with <addr>
    - Line numbers in errors: Normalize to <line>
    - Platform paths: Normalize separators
    - Whitespace: Normalize trailing whitespace
    """
    if strict:
        return text
    
    result = text
    
    # Remove NovaSharp CLI compatibility info lines
    result = re.sub(r'^\[compatibility\].*$\n?', '', result, flags=re.MULTILINE)
    
    # Normalize version strings (_VERSION output differs between Lua and NovaSharp)
    # Lua outputs "Lua 5.x" while NovaSharp outputs "NovaSharp x.x.x.x"
    result = re.sub(r'Lua 5\.\d+', '<lua-version>', result)
    result = re.sub(r'NovaSharp \d+\.\d+\.\d+\.\d+', '<lua-version>', result)
    
    # Normalize floating-point numbers (e.g., 1.0000000000001 -> 1.0)
    def normalize_float(match):
        try:
            num = float(match.group(0))
            # Handle very small numbers near zero
            if abs(num) < 1e-10:
                return "0"
            # Round to reasonable precision
            rounded = round(num, 10)
            # Format without unnecessary trailing zeros
            if rounded == int(rounded):
                return str(int(rounded))
            return str(rounded).rstrip('0').rstrip('.')
        except ValueError:
            return match.group(0)
    
    # Match floating-point numbers (including scientific notation)
    result = re.sub(r'-?\d+\.\d+(?:e[+-]?\d+)?', normalize_float, result)
    
    # Normalize memory addresses (0x7f... -> <addr>)
    result = re.sub(r'0x[0-9a-fA-F]+', '<addr>', result)
    
    # Normalize NovaSharp-style addresses (no 0x prefix, e.g., 00000BD3ABCD1234)
    # Match 8+ hex digits that look like addresses (typically 8-16 digits)
    result = re.sub(r'(?<=[:\s])[0-9A-F]{8,16}(?=[:\s\n]|$)', '<addr>', result)
    
    # Normalize table addresses in Lua output (table: 0x... -> table: <addr>)
    result = re.sub(r'(table|function|userdata|thread): <addr>', r'\1: <addr>', result)
    
    # Normalize line numbers in error messages (file.lua:123: -> file.lua:<line>:)
    result = re.sub(r'(\.lua):(\d+):', r'\1:<line>:', result)
    
    # Normalize stack trace line numbers
    result = re.sub(r'\[C\]:\s*-?\d+:', '[C]:<line>:', result)
    result = re.sub(r'\[string "[^"]*"\]:\d+:', '[string "<chunk>"]:<line>:', result)
    
    # Normalize NovaSharp-specific vs Lua-specific error message prefixes
    result = re.sub(r'^lua\d?\.\d?: ', '', result, flags=re.MULTILINE)
    
    # Normalize debug prompts (lua_debug> vs full path prefixes)
    result = re.sub(r'^lua_debug>', '<debug>', result, flags=re.MULTILINE)
    result = re.sub(r'^/[^\s:]+:', '<path>:', result, flags=re.MULTILINE)
    
    # Normalize .NET stack traces to generic format
    result = re.sub(r'^\s+at NovaSharp\..*$', '  <stack-frame>', result, flags=re.MULTILINE)
    result = re.sub(r'^Unhandled exception\. NovaSharp\.Interpreter\.Errors\.', '', result, flags=re.MULTILINE)
    
    # Collapse multiple consecutive stack frames
    result = re.sub(r'(<stack-frame>\n)+', '<stack-trace>\n', result)
    
    # Normalize path separators
    result = result.replace('\\', '/')
    
    # Normalize trailing whitespace
    result = '\n'.join(line.rstrip() for line in result.split('\n'))
    
    # Normalize multiple blank lines to single
    result = re.sub(r'\n{3,}', '\n\n', result)
    
    # Strip trailing newlines
    result = result.rstrip('\n')
    
    return result


def read_file_safe(path: Path) -> str:
    """Read file contents, returning empty string if file doesn't exist."""
    try:
        return path.read_text(encoding='utf-8', errors='replace')
    except (FileNotFoundError, IOError):
        return ""


def compare_snippet(
    results_dir: Path,
    rel_path: str,
    lua_version: str,
    strict: bool = False
) -> ComparisonResult:
    """Compare outputs for a single Lua snippet."""
    base_path = results_dir / rel_path.replace('.lua', '')
    
    # Read Lua outputs
    lua_out = read_file_safe(base_path.with_suffix(f'.lua{lua_version}.out'))
    lua_err = read_file_safe(base_path.with_suffix(f'.lua{lua_version}.err'))
    lua_rc_str = read_file_safe(base_path.with_suffix(f'.lua{lua_version}.rc'))
    lua_rc = int(lua_rc_str.strip()) if lua_rc_str.strip() else -1
    
    # Read NovaSharp outputs
    nova_out = read_file_safe(base_path.with_suffix('.nova.out'))
    nova_err = read_file_safe(base_path.with_suffix('.nova.err'))
    nova_rc_str = read_file_safe(base_path.with_suffix('.nova.rc'))
    nova_rc = int(nova_rc_str.strip()) if nova_rc_str.strip() else -1
    
    result = ComparisonResult(
        file=rel_path,
        lua_version=lua_version,
        lua_output=lua_out,
        nova_output=nova_out,
        lua_error=lua_err,
        nova_error=nova_err,
        lua_rc=lua_rc,
        nova_rc=nova_rc,
        status="unknown"
    )
    
    # Determine if outputs were generated
    has_lua = lua_rc != -1
    has_nova = nova_rc != -1
    
    if not has_lua and not has_nova:
        result.status = "skipped"
        return result
    
    if not has_lua:
        result.status = "nova_only"
        return result
    
    if not has_nova:
        result.status = "lua_only"
        return result
    
    # Both ran - compare outputs
    # Combine stdout and stderr for comparison (many Lua errors go to stderr)
    lua_combined = (lua_out + "\n" + lua_err).strip()
    nova_combined = (nova_out + "\n" + nova_err).strip()
    
    # Exact match
    if lua_combined == nova_combined:
        result.status = "match"
        result.normalized_match = True
        return result
    
    # Normalized match
    lua_normalized = normalize_output(lua_combined, strict)
    nova_normalized = normalize_output(nova_combined, strict)
    
    if lua_normalized == nova_normalized:
        result.status = "match"
        result.normalized_match = True
        result.diff_summary = "Matched after normalization"
        return result
    
    # Both errored with different messages
    if lua_rc != 0 and nova_rc != 0:
        result.status = "both_error"
        result.diff_summary = "Both errored with different messages"
        return result
    
    # Mismatch
    result.status = "mismatch"
    
    # Generate diff summary
    lua_lines = lua_normalized.split('\n')
    nova_lines = nova_normalized.split('\n')
    
    if len(lua_lines) != len(nova_lines):
        result.diff_summary = f"Line count differs: Lua={len(lua_lines)}, Nova={len(nova_lines)}"
    else:
        # Find first differing line
        for i, (lua_line, nova_line) in enumerate(zip(lua_lines, nova_lines)):
            if lua_line != nova_line:
                result.diff_summary = f"First diff at line {i+1}: Lua='{lua_line[:50]}...', Nova='{nova_line[:50]}...'"
                break
    
    return result


def find_lua_files(corpus_dir: Path) -> list[str]:
    """Find all Lua files in corpus, returning relative paths."""
    files = []
    for lua_file in corpus_dir.rglob('*.lua'):
        rel_path = str(lua_file.relative_to(corpus_dir))
        files.append(rel_path)
    return sorted(files)


def main():
    parser = argparse.ArgumentParser(
        description="Compare Lua execution outputs with semantic normalization"
    )
    parser.add_argument(
        '--results-dir',
        type=Path,
        default=Path('artifacts/lua-corpus-results'),
        help='Directory with execution results'
    )
    parser.add_argument(
        '--corpus-dir',
        type=Path,
        default=Path('artifacts/lua-corpus'),
        help='Directory with original Lua corpus (for file listing)'
    )
    parser.add_argument(
        '--output-file',
        type=Path,
        default=None,
        help='Output comparison report (default: results-dir/comparison.json)'
    )
    parser.add_argument(
        '--lua-version',
        default='5.4',
        choices=['5.1', '5.2', '5.3', '5.4', '5.5'],
        help='Lua version to compare against'
    )
    parser.add_argument(
        '--verbose', '-v',
        action='store_true',
        help='Show detailed differences'
    )
    parser.add_argument(
        '--strict',
        action='store_true',
        help="Don't apply semantic normalization"
    )
    parser.add_argument(
        '--allowlist',
        type=Path,
        default=None,
        help='JSON file with additional known divergences to exclude from failure'
    )
    parser.add_argument(
        '--enforce',
        action='store_true',
        help='Exit with error on unexpected mismatches (for CI gating)'
    )
    parser.add_argument(
        '--monitor',
        action='store_true',
        help='Monitor mode - report results without failing (for experimental versions like 5.5)'
    )
    
    args = parser.parse_args()
    
    # Load additional allowlist if provided
    allowlist = set(KNOWN_DIVERGENCES)
    if args.allowlist and args.allowlist.exists():
        with open(args.allowlist, 'r', encoding='utf-8') as f:
            allowlist.update(json.load(f))
    
    if args.output_file is None:
        args.output_file = args.results_dir / 'comparison.json'
    
    if not args.results_dir.exists():
        print(f"Error: Results directory not found: {args.results_dir}", file=sys.stderr)
        print("Run 'scripts/tests/run-lua-corpus.sh' first.", file=sys.stderr)
        sys.exit(1)
    
    # Find all Lua files
    if args.corpus_dir.exists():
        lua_files = find_lua_files(args.corpus_dir)
    else:
        # Fall back to finding output files
        lua_files = []
        for out_file in args.results_dir.rglob(f'*.lua{args.lua_version}.out'):
            rel = str(out_file.relative_to(args.results_dir))
            rel = rel.replace(f'.lua{args.lua_version}.out', '.lua')
            lua_files.append(rel)
        lua_files = sorted(set(lua_files))
    
    print(f"Comparing {len(lua_files)} snippets against Lua {args.lua_version}")
    print(f"Results directory: {args.results_dir}")
    print(f"Normalization: {'disabled' if args.strict else 'enabled'}")
    print(f"Known divergences: {len(allowlist)}")
    print(f"Enforce mode: {'enabled' if args.enforce else 'disabled'}")
    print()
    
    # Compare all snippets
    results: list[ComparisonResult] = []
    stats = {
        'match': 0,
        'mismatch': 0,
        'known_divergence': 0,
        'both_error': 0,
        'lua_only': 0,
        'nova_only': 0,
        'skipped': 0,
    }
    
    for i, rel_path in enumerate(lua_files):
        result = compare_snippet(
            args.results_dir, rel_path, args.lua_version, args.strict
        )
        
        # Check fixture version compatibility before flagging as mismatch
        if result.status == 'mismatch':
            # Try to find the fixture file and check its version metadata
            fixture_path = DEFAULT_FIXTURES_DIR / rel_path
            if fixture_path.exists():
                lua_versions, novasharp_only = parse_fixture_version_info(fixture_path)
                if not is_fixture_compatible(lua_versions, args.lua_version, novasharp_only):
                    # This fixture should not have been run against this version
                    result.status = 'skipped'
                    result.diff_summary = f"Version incompatible: fixture targets {lua_versions}"
        
        # Check if this is a known divergence
        if result.status == 'mismatch' and rel_path in allowlist:
            result.status = 'known_divergence'
        
        results.append(result)
        stats[result.status] = stats.get(result.status, 0) + 1
        
        if args.verbose and result.status in ('mismatch', 'both_error'):
            print(f"[{result.status.upper()}] {rel_path}")
            if result.diff_summary:
                print(f"  {result.diff_summary}")
        
        if (i + 1) % 100 == 0:
            print(f"Processed {i + 1}/{len(lua_files)} snippets...")
    
    # Print summary
    print()
    print("=== Comparison Summary ===")
    print(f"Lua version:    {args.lua_version}")
    print(f"Total:          {len(results)}")
    print(f"Match:          {stats['match']}")
    print(f"Mismatch:       {stats['mismatch']}")
    print(f"Known divergence: {stats['known_divergence']}")
    print(f"Both error:     {stats['both_error']}")
    print(f"Lua only:       {stats['lua_only']}")
    print(f"Nova only:      {stats['nova_only']}")
    print(f"Skipped:        {stats['skipped']}")
    
    # Calculate match rate (excluding skipped, partial runs, and known divergences)
    comparable = stats['match'] + stats['mismatch'] + stats['both_error'] + stats['known_divergence']
    effective_matches = stats['match'] + stats['known_divergence']
    if comparable > 0:
        match_rate = (effective_matches / comparable) * 100
        print(f"\nEffective match rate: {match_rate:.1f}% ({effective_matches}/{comparable})")
        if stats['mismatch'] > 0:
            print(f"Unexpected mismatches: {stats['mismatch']}")
    
    # Write JSON report
    report = {
        'lua_version': args.lua_version,
        'strict_mode': args.strict,
        'enforce_mode': args.enforce,
        'summary': stats,
        'match_rate': match_rate if comparable > 0 else None,
        'mismatches': [
            {
                'file': r.file,
                'diff_summary': r.diff_summary,
                'lua_rc': r.lua_rc,
                'nova_rc': r.nova_rc,
            }
            for r in results
            if r.status == 'mismatch'
        ][:100],  # Limit to first 100 mismatches
        'both_errors': [
            {
                'file': r.file,
                'lua_error': r.lua_error[:500] if r.lua_error else '',
                'nova_error': r.nova_error[:500] if r.nova_error else '',
            }
            for r in results
            if r.status == 'both_error'
        ][:50],  # Limit to first 50
        'known_divergences': [
            {
                'file': r.file,
                'diff_summary': r.diff_summary,
            }
            for r in results
            if r.status == 'known_divergence'
        ],
    }
    
    args.output_file.parent.mkdir(parents=True, exist_ok=True)
    with open(args.output_file, 'w', encoding='utf-8') as f:
        json.dump(report, f, indent=2)
    
    print(f"\nReport written to: {args.output_file}")
    
    # Exit logic based on mode
    if args.monitor:
        # Monitor mode: always succeed but report status
        if stats['mismatch'] > 0:
            print(f"\n[INFO] MONITOR MODE: {stats['mismatch']} mismatch(es) found (not failing)")
            print("This is expected for experimental Lua versions like 5.5.")
        else:
            print("\n[OK] MONITOR MODE: All comparable fixtures match.")
        sys.exit(0)
    elif args.enforce and stats['mismatch'] > 0:
        print(f"\n[FAIL] ENFORCE MODE: {stats['mismatch']} unexpected mismatch(es) found!")
        print("Add to KNOWN_DIVERGENCES in compare-lua-outputs.py if these are expected,")
        print("or fix the divergence in NovaSharp runtime.")
        sys.exit(1)
    elif stats['mismatch'] > 0:
        print(f"\n[WARN] {stats['mismatch']} mismatch(es) found (warn mode, not failing)")
        sys.exit(0)
    else:
        print("\n[OK] All comparable fixtures match (or are documented divergences).")
        sys.exit(0)


if __name__ == '__main__':
    main()
