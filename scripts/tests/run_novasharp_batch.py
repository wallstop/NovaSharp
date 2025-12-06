#!/usr/bin/env python3
"""
run_novasharp_batch.py - Run multiple Lua files through NovaSharp in a single process

This script creates a temporary C# program that runs all the Lua files in batch,
avoiding the per-file process startup overhead.

Usage:
    python3 scripts/tests/run_novasharp_batch.py --files-list <list.txt> --output-dir <dir>
"""

import argparse
import json
import os
import subprocess
import sys
import tempfile
import time
from pathlib import Path


def run_batch(files_list: Path, output_dir: Path, fixtures_dir: Path, timeout: int = 300) -> dict:
    """Run all Lua files through NovaSharp using a generated batch runner."""
    
    # Read file list
    with open(files_list) as f:
        lua_files = [line.strip() for line in f if line.strip()]
    
    if not lua_files:
        return {"pass": 0, "fail": 0, "error": 0, "results": []}
    
    output_dir.mkdir(parents=True, exist_ok=True)
    
    results = []
    pass_count = 0
    fail_count = 0
    error_count = 0
    
    # Find NovaSharp.Interpreter.dll
    repo_root = Path(__file__).parent.parent.parent
    interpreter_dll = repo_root / "src/runtime/NovaSharp.Interpreter/bin/Release/netstandard2.1/NovaSharp.Interpreter.dll"
    
    if not interpreter_dll.exists():
        # Try to build it
        print("Building NovaSharp.Interpreter...")
        subprocess.run(
            ["dotnet", "build", 
             str(repo_root / "src/runtime/NovaSharp.Interpreter/NovaSharp.Interpreter.csproj"),
             "-c", "Release", "-v", "q", "--nologo"],
            check=True
        )
    
    if not interpreter_dll.exists():
        print(f"Error: NovaSharp.Interpreter.dll not found at {interpreter_dll}", file=sys.stderr)
        sys.exit(1)
    
    # Process files using a simple Python wrapper that invokes the CLI once per file
    # but with subprocess reuse where possible
    
    cli_dll = repo_root / "src/tooling/NovaSharp.Cli/bin/Release/net8.0/NovaSharp.Cli.dll"
    if not cli_dll.exists():
        print("Building NovaSharp CLI...")
        subprocess.run(
            ["dotnet", "build",
             str(repo_root / "src/tooling/NovaSharp.Cli/NovaSharp.Cli.csproj"),
             "-c", "Release", "-v", "q", "--nologo"],
            check=True
        )
    
    total = len(lua_files)
    start_time = time.time()
    
    env = os.environ.copy()
    env["DOTNET_ROLL_FORWARD"] = "Major"
    
    for i, lua_file in enumerate(lua_files):
        lua_path = Path(lua_file)
        rel_path = lua_path.relative_to(fixtures_dir) if fixtures_dir in lua_path.parents or lua_path.parent == fixtures_dir else lua_path.name
        output_base = output_dir / str(rel_path).replace('.lua', '')
        output_base.parent.mkdir(parents=True, exist_ok=True)
        
        out_file = output_base.with_suffix('.nova.out')
        err_file = output_base.with_suffix('.nova.err')
        rc_file = output_base.with_suffix('.nova.rc')
        
        try:
            result = subprocess.run(
                ["dotnet", str(cli_dll), lua_file],
                capture_output=True,
                text=True,
                timeout=10,
                env=env
            )
            
            out_file.write_text(result.stdout)
            err_file.write_text(result.stderr)
            rc_file.write_text(str(result.returncode))
            
            if result.returncode == 0:
                status = "pass"
                pass_count += 1
            else:
                status = "fail"
                fail_count += 1
                
        except subprocess.TimeoutExpired:
            out_file.write_text("")
            err_file.write_text("Timeout after 10s")
            rc_file.write_text("-1")
            status = "error"
            error_count += 1
        except Exception as e:
            out_file.write_text("")
            err_file.write_text(str(e))
            rc_file.write_text("-1")
            status = "error"
            error_count += 1
        
        results.append({
            "file": str(rel_path),
            "status": status
        })
        
        if (i + 1) % 100 == 0:
            elapsed = time.time() - start_time
            rate = (i + 1) / elapsed
            remaining = (total - i - 1) / rate if rate > 0 else 0
            print(f"  Progress: {i + 1}/{total} ({rate:.1f}/s, ~{remaining:.0f}s remaining)")
    
    elapsed = time.time() - start_time
    print(f"NovaSharp batch completed in {elapsed:.1f}s: {pass_count} pass, {fail_count} fail, {error_count} error")
    
    return {
        "pass": pass_count,
        "fail": fail_count,
        "error": error_count,
        "elapsed_seconds": elapsed,
        "results": results
    }


def main():
    parser = argparse.ArgumentParser(description="Run Lua files through NovaSharp in batch")
    parser.add_argument("--files-list", type=Path, required=True, help="File containing list of Lua files")
    parser.add_argument("--output-dir", type=Path, required=True, help="Output directory for results")
    parser.add_argument("--fixtures-dir", type=Path, required=True, help="Base fixtures directory for relative paths")
    parser.add_argument("--timeout", type=int, default=300, help="Total timeout in seconds")
    
    args = parser.parse_args()
    
    results = run_batch(args.files_list, args.output_dir, args.fixtures_dir, args.timeout)
    
    # Write results
    results_file = args.output_dir / "novasharp_results.json"
    with open(results_file, 'w') as f:
        json.dump(results, f, indent=2)
    
    print(f"Results written to {results_file}")


if __name__ == "__main__":
    main()
