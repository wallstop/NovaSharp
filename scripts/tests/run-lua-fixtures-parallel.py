#!/usr/bin/env python3
"""Parallel Lua fixture runner for comparing NovaSharp against reference Lua.

This script runs Lua fixture files against both the reference Lua interpreter
and NovaSharp in parallel for much faster execution than the sequential bash version.

Usage:
    python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version 5.1
    python3 scripts/tests/run-lua-fixtures-parallel.py --lua-version 5.4 --workers 8
"""

from __future__ import annotations

import argparse
import json
import os
import re
import subprocess
import sys
import tempfile
import time
from concurrent.futures import ProcessPoolExecutor, as_completed
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_FIXTURES_DIR = ROOT / "src" / "tests" / "WallstopStudios.NovaSharp.Interpreter.Tests" / "LuaFixtures"
DEFAULT_OUTPUT_DIR = ROOT / "artifacts" / "lua-comparison-results"
CLI_PROJECT = ROOT / "src" / "tooling" / "WallstopStudios.NovaSharp.Cli" / "WallstopStudios.NovaSharp.Cli.csproj"


@dataclass
class FixtureResult:
    """Result of running a single fixture."""
    file: str
    lua_version: str
    lua_status: str = "skipped"
    nova_status: str = "skipped"
    expects_error: bool = False
    lua_output: str = ""
    lua_error: str = ""
    lua_rc: int = 0
    nova_output: str = ""
    nova_error: str = ""
    nova_rc: int = 0
    skipped_reason: Optional[str] = None


@dataclass
class FixtureMetadata:
    """Metadata extracted from a fixture file."""
    path: Path
    lua_versions: list[str] = field(default_factory=list)
    novasharp_only: bool = False
    expects_error: bool = False
    
    def is_compatible(self, version: str) -> bool:
        """Check if this fixture is compatible with the given Lua version."""
        if self.novasharp_only:
            return False
        
        if not self.lua_versions:
            return True  # No version info, assume compatible
        
        # Check for exact match
        if version in self.lua_versions:
            return True
        
        # Check for "5.1+" style patterns
        version_num = int(version.replace(".", ""))
        for v in self.lua_versions:
            if v.endswith("+"):
                base_version = int(v.rstrip("+").replace(".", ""))
                if version_num >= base_version:
                    return True
        
        return False


def parse_fixture_metadata(path: Path) -> FixtureMetadata:
    """Parse metadata from a fixture file header."""
    meta = FixtureMetadata(path=path)
    
    try:
        with open(path, 'r', encoding='utf-8') as f:
            # Read first 10 lines for metadata
            for _ in range(10):
                line = f.readline()
                if not line.startswith("--"):
                    break
                
                if "@lua-versions:" in line:
                    versions_part = line.split("@lua-versions:")[1].strip()
                    if "novasharp-only" in versions_part:
                        meta.novasharp_only = True
                    else:
                        # Parse versions like "5.1, 5.2, 5.3" or "5.1+"
                        meta.lua_versions = [v.strip() for v in versions_part.split(",")]
                
                if "@novasharp-only: true" in line:
                    meta.novasharp_only = True
                
                if "@expects-error: true" in line:
                    meta.expects_error = True
    except Exception:
        pass
    
    return meta


def run_lua(lua_cmd: str, fixture_path: Path, timeout_seconds: int = 5) -> tuple[int, str, str]:
    """Run a fixture with reference Lua."""
    try:
        result = subprocess.run(
            [lua_cmd, str(fixture_path)],
            capture_output=True,
            text=True,
            timeout=timeout_seconds
        )
        return result.returncode, result.stdout, result.stderr
    except subprocess.TimeoutExpired:
        return -1, "", "Timeout"
    except Exception as e:
        return -1, "", str(e)


def run_novasharp_single(nova_project: str, fixture_path: Path, lua_version: str, timeout_seconds: int = 10) -> tuple[int, str, str]:
    """Run a fixture with NovaSharp."""
    try:
        # Check if it's a DLL or a project file
        if nova_project.endswith(".dll"):
            cmd = ["dotnet", nova_project, "--lua-version", lua_version, str(fixture_path)]
        else:
            # Use dotnet run for project files
            cmd = ["dotnet", "run", "--project", nova_project, "-c", "Release", 
                   "--framework", "net8.0", "--no-build", "--", 
                   "--lua-version", lua_version, str(fixture_path)]
        
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=timeout_seconds,
            env={**os.environ, "DOTNET_ROLL_FORWARD": "Major"}
        )
        return result.returncode, result.stdout, result.stderr
    except subprocess.TimeoutExpired:
        return -1, "", "Timeout"
    except Exception as e:
        return -1, "", str(e)


def process_fixture(args: tuple) -> FixtureResult:
    """Process a single fixture (worker function for parallel execution)."""
    fixture_path, lua_version, lua_cmd, nova_project, output_dir, skip_lua, skip_nova = args
    
    meta = parse_fixture_metadata(fixture_path)
    rel_path = fixture_path.relative_to(DEFAULT_FIXTURES_DIR)
    
    result = FixtureResult(
        file=str(rel_path),
        lua_version=lua_version,
        expects_error=meta.expects_error
    )
    
    # Check compatibility
    if not meta.is_compatible(lua_version):
        if meta.novasharp_only:
            result.skipped_reason = "novasharp-only"
        else:
            result.skipped_reason = "version-incompatible"
        return result
    
    # Create output directory for this fixture
    output_base = output_dir / rel_path.with_suffix("")
    output_base.parent.mkdir(parents=True, exist_ok=True)
    
    # Run reference Lua
    if not skip_lua and lua_cmd:
        rc, stdout, stderr = run_lua(lua_cmd, fixture_path)
        result.lua_rc = rc
        result.lua_output = stdout
        result.lua_error = stderr
        
        # For expects-error tests, non-zero return code is success
        if meta.expects_error:
            result.lua_status = "pass" if rc != 0 else "fail"
        else:
            result.lua_status = "pass" if rc == 0 else "fail"
        
        # Write output files
        (output_base.parent / f"{output_base.name}.lua{lua_version}.out").write_text(stdout)
        (output_base.parent / f"{output_base.name}.lua{lua_version}.err").write_text(stderr)
        (output_base.parent / f"{output_base.name}.lua{lua_version}.rc").write_text(str(rc))
    
    # Run NovaSharp
    if not skip_nova and nova_project:
        rc, stdout, stderr = run_novasharp_single(nova_project, fixture_path, lua_version)
        result.nova_rc = rc
        result.nova_output = stdout
        result.nova_error = stderr
        
        # For expects-error tests, non-zero return code is success
        if meta.expects_error:
            result.nova_status = "pass" if rc != 0 else "fail"
        else:
            result.nova_status = "pass" if rc == 0 else "fail"
        
        # Write output files
        (output_base.parent / f"{output_base.name}.nova.out").write_text(stdout)
        (output_base.parent / f"{output_base.name}.nova.err").write_text(stderr)
        (output_base.parent / f"{output_base.name}.nova.rc").write_text(str(rc))
    
    return result


def build_novasharp(cli_project: Path) -> Optional[str]:
    """Build NovaSharp and return path to the DLL."""
    print("Building NovaSharp CLI...")
    result = subprocess.run(
        ["dotnet", "build", str(cli_project), "-c", "Release", "-v", "q", "--nologo"],
        capture_output=True,
        text=True
    )
    
    if result.returncode != 0:
        print(f"Build failed: {result.stderr}", file=sys.stderr)
        return None
    
    # Find the built DLL
    dll_path = cli_project.parent / "bin" / "Release" / "net8.0" / "WallstopStudios.NovaSharp.Cli.dll"
    
    if dll_path.exists():
        return str(dll_path)
    
    # Try other framework versions
    for framework in ["net9.0", "net7.0", "net6.0"]:
        alt_path = cli_project.parent / "bin" / "Release" / framework / "WallstopStudios.NovaSharp.Cli.dll"
        if alt_path.exists():
            return str(alt_path)
    
    # Fallback to project path
    return str(cli_project)


def run_novasharp_batch_via_dotnet(nova_project: str, fixtures: list[Path], lua_version: str, output_dir: Path, workers: int) -> dict[Path, tuple[int, str, str]]:
    """Run NovaSharp on multiple fixtures using dotnet exec for better performance."""
    results = {}
    
    # Use ProcessPoolExecutor to run multiple instances
    def run_one(fixture_path: Path) -> tuple[Path, int, str, str]:
        try:
            # Use dotnet exec for slightly faster startup than dotnet run
            result = subprocess.run(
                ["dotnet", "exec", nova_project, "--lua-version", lua_version, str(fixture_path)],
                capture_output=True,
                text=True,
                timeout=10,
                env={**os.environ, "DOTNET_ROLL_FORWARD": "Major"}
            )
            return fixture_path, result.returncode, result.stdout, result.stderr
        except subprocess.TimeoutExpired:
            return fixture_path, -1, "", "Timeout"
        except Exception as e:
            return fixture_path, -1, "", str(e)
    
    with ProcessPoolExecutor(max_workers=workers) as executor:
        futures = {executor.submit(run_one, f): f for f in fixtures}
        for future in as_completed(futures):
            path, rc, stdout, stderr = future.result()
            results[path] = (rc, stdout, stderr)
    
    return results


def main():
    parser = argparse.ArgumentParser(description="Parallel Lua fixture runner")
    parser.add_argument("--fixtures-dir", type=Path, default=DEFAULT_FIXTURES_DIR,
                        help="Directory containing Lua fixtures")
    parser.add_argument("--output-dir", type=Path, default=DEFAULT_OUTPUT_DIR,
                        help="Directory for comparison results")
    parser.add_argument("--lua-version", default="5.4",
                        help="Lua version to test: 5.1, 5.2, 5.3, 5.4")
    parser.add_argument("--lua-cmd", default=None,
                        help="Override Lua command")
    parser.add_argument("--skip-novasharp", action="store_true",
                        help="Skip NovaSharp execution")
    parser.add_argument("--skip-lua", action="store_true",
                        help="Skip reference Lua execution")
    parser.add_argument("--limit", type=int, default=0,
                        help="Limit number of fixtures to process")
    parser.add_argument("--workers", "-j", type=int, default=None,
                        help="Number of parallel workers (default: CPU count)")
    parser.add_argument("--verbose", "-v", action="store_true",
                        help="Print detailed progress")
    
    args = parser.parse_args()
    
    # Set defaults
    lua_cmd = args.lua_cmd or f"lua{args.lua_version}"
    workers = args.workers or os.cpu_count() or 4
    
    # Verify fixtures exist
    if not args.fixtures_dir.exists():
        print(f"Error: Fixtures directory not found: {args.fixtures_dir}", file=sys.stderr)
        print("Run 'python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py' first.", file=sys.stderr)
        sys.exit(1)
    
    # Verify Lua is available
    if not args.skip_lua:
        try:
            subprocess.run([lua_cmd, "-v"], capture_output=True, check=True)
        except (subprocess.CalledProcessError, FileNotFoundError):
            print(f"Error: Lua {args.lua_version} not found ({lua_cmd}).", file=sys.stderr)
            print(f"Install with 'sudo apt-get install lua{args.lua_version}'", file=sys.stderr)
            sys.exit(1)
    
    # Build NovaSharp
    nova_exe = None
    if not args.skip_novasharp:
        nova_exe = build_novasharp(CLI_PROJECT)
        if not nova_exe:
            print("Error: Failed to build NovaSharp", file=sys.stderr)
            sys.exit(1)
    
    # Create output directory
    args.output_dir.mkdir(parents=True, exist_ok=True)
    
    # Find all fixtures
    all_fixtures = sorted(args.fixtures_dir.glob("**/*.lua"))
    print(f"Found {len(all_fixtures)} Lua fixture files")
    print(f"Testing against Lua {args.lua_version} ({lua_cmd})")
    print(f"Using {workers} parallel workers")
    print(f"Output directory: {args.output_dir}")
    print()
    
    # Apply limit
    if args.limit > 0:
        all_fixtures = all_fixtures[:args.limit]
    
    # Process fixtures in parallel
    start_time = time.time()
    
    # Prepare work items
    work_items = [
        (f, args.lua_version, lua_cmd if not args.skip_lua else None, 
         nova_exe, args.output_dir, args.skip_lua, args.skip_novasharp)
        for f in all_fixtures
    ]
    
    results: list[FixtureResult] = []
    
    with ProcessPoolExecutor(max_workers=workers) as executor:
        futures = {executor.submit(process_fixture, item): item[0] for item in work_items}
        
        completed = 0
        for future in as_completed(futures):
            completed += 1
            result = future.result()
            results.append(result)
            
            if args.verbose:
                status = result.skipped_reason or f"lua={result.lua_status} nova={result.nova_status}"
                print(f"[{completed}/{len(all_fixtures)}] {result.file}: {status}")
            elif completed % 100 == 0:
                print(f"Progress: {completed}/{len(all_fixtures)}")
    
    elapsed = time.time() - start_time
    
    # Calculate summary
    total = len(results)
    compatible = sum(1 for r in results if r.skipped_reason is None)
    skipped_version = sum(1 for r in results if r.skipped_reason == "version-incompatible")
    skipped_novasharp = sum(1 for r in results if r.skipped_reason == "novasharp-only")
    lua_pass = sum(1 for r in results if r.lua_status == "pass")
    lua_fail = sum(1 for r in results if r.lua_status == "fail")
    nova_pass = sum(1 for r in results if r.nova_status == "pass")
    nova_fail = sum(1 for r in results if r.nova_status == "fail")
    
    # Write results JSON
    results_file = args.output_dir / "results.json"
    output_data = {
        "results": [
            {
                "file": r.file,
                "lua_version": r.lua_version,
                "lua_status": r.lua_status,
                "nova_status": r.nova_status,
                "expects_error": r.expects_error,
                "skipped_reason": r.skipped_reason
            }
            for r in sorted(results, key=lambda r: r.file)
        ],
        "summary": {
            "lua_version": args.lua_version,
            "total": total,
            "compatible": compatible,
            "skipped_version": skipped_version,
            "skipped_novasharp": skipped_novasharp,
            "lua_pass": lua_pass,
            "lua_fail": lua_fail,
            "nova_pass": nova_pass,
            "nova_fail": nova_fail,
            "elapsed_seconds": round(elapsed, 2),
            "workers": workers
        }
    }
    
    with open(results_file, 'w', encoding='utf-8') as f:
        json.dump(output_data, f, indent=2)
    
    # Print summary
    print()
    print("=== Lua Fixture Test Summary ===")
    print(f"Lua version:           {args.lua_version} ({lua_cmd})")
    print(f"Total fixtures:        {total}")
    print(f"Compatible:            {compatible}")
    print(f"Skipped (version):     {skipped_version}")
    print(f"Skipped (NovaSharp):   {skipped_novasharp}")
    if not args.skip_lua:
        print(f"Lua {args.lua_version} pass:          {lua_pass}")
        print(f"Lua {args.lua_version} fail:          {lua_fail}")
    if not args.skip_novasharp:
        print(f"NovaSharp pass:        {nova_pass}")
        print(f"NovaSharp fail:        {nova_fail}")
    print(f"Elapsed time:          {elapsed:.2f}s")
    print(f"Fixtures/second:       {total / elapsed:.1f}")
    print()
    print(f"Results written to: {results_file}")


if __name__ == "__main__":
    main()
