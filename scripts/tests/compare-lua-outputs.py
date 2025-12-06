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
    --results-dir DIR     Directory with execution results (default: artifacts/lua-corpus-results)
    --output-file FILE    Output comparison report (default: artifacts/lua-corpus-results/comparison.json)
    --lua-version VER     Lua version to compare against (default: 5.4)
    --verbose             Show detailed differences
    --strict              Don't apply semantic normalization
"""

import argparse
import json
import os
import re
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional


@dataclass
class ComparisonResult:
    """Result of comparing a single snippet's outputs."""
    file: str
    lua_version: str
    status: str  # "match", "mismatch", "lua_only", "nova_only", "both_error", "skipped"
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
    
    # Normalize table addresses in Lua output (table: 0x... -> table: <addr>)
    result = re.sub(r'(table|function|userdata|thread): <addr>', r'\1: <addr>', result)
    
    # Normalize line numbers in error messages (file.lua:123: -> file.lua:<line>:)
    result = re.sub(r'(\.lua):(\d+):', r'\1:<line>:', result)
    
    # Normalize stack trace line numbers
    result = re.sub(r'\[C\]:\s*-?\d+:', '[C]:<line>:', result)
    result = re.sub(r'\[string "[^"]*"\]:\d+:', '[string "<chunk>"]:<line>:', result)
    
    # Normalize NovaSharp-specific vs Lua-specific error message prefixes
    result = re.sub(r'^lua\d?\.\d?: ', '', result, flags=re.MULTILINE)
    
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
        choices=['5.1', '5.2', '5.3', '5.4'],
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
    
    args = parser.parse_args()
    
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
    print()
    
    # Compare all snippets
    results: list[ComparisonResult] = []
    stats = {
        'match': 0,
        'mismatch': 0,
        'both_error': 0,
        'lua_only': 0,
        'nova_only': 0,
        'skipped': 0,
    }
    
    for i, rel_path in enumerate(lua_files):
        result = compare_snippet(
            args.results_dir, rel_path, args.lua_version, args.strict
        )
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
    print(f"Both error:     {stats['both_error']}")
    print(f"Lua only:       {stats['lua_only']}")
    print(f"Nova only:      {stats['nova_only']}")
    print(f"Skipped:        {stats['skipped']}")
    
    # Calculate match rate (excluding skipped and partial runs)
    comparable = stats['match'] + stats['mismatch'] + stats['both_error']
    if comparable > 0:
        match_rate = (stats['match'] / comparable) * 100
        print(f"\nMatch rate:     {match_rate:.1f}% ({stats['match']}/{comparable})")
    
    # Write JSON report
    report = {
        'lua_version': args.lua_version,
        'strict_mode': args.strict,
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
    }
    
    args.output_file.parent.mkdir(parents=True, exist_ok=True)
    with open(args.output_file, 'w', encoding='utf-8') as f:
        json.dump(report, f, indent=2)
    
    print(f"\nReport written to: {args.output_file}")
    
    # Exit with error if there are mismatches
    if stats['mismatch'] > 0:
        sys.exit(1)
    
    sys.exit(0)


if __name__ == '__main__':
    main()
