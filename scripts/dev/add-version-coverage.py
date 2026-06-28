#!/usr/bin/env python3
"""
Script to add LuaCompatibilityVersion coverage to TUnit test files.

This script:
1. Adds version Arguments attributes to test methods without them
2. Changes method signatures to accept LuaCompatibilityVersion version parameter
3. Replaces CreateScript() calls with CreateScriptWithVersion(version) in modified methods
4. Preserves existing version-specific tests

Usage:
    python3 scripts/dev/add-version-coverage.py <file_path>
"""

import re
import sys
from pathlib import Path


def add_version_coverage(file_path: str) -> int:
    """
    Add version coverage to a TUnit test file.
    Returns the number of methods modified.
    """
    with open(file_path, 'r') as f:
        content = f.read()

    lines = content.split('\n')
    result = []
    i = 0

    # Define version attributes block
    version_attrs = '''        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]'''

    test_marker = re.compile(r'^(\s+)\[global::TUnit\.Core\.Test\]\s*$')
    method_marker = re.compile(r'^(\s+)(public async Task )(\w+)(\(\))$')
    create_script_patterns = [
        (re.compile(r'^(\s+)(Script script = CreateScript\(\);)$'), 'CreateScriptWithVersion(version);'),
        (re.compile(r'^(\s+)(Script script = new Script\(CoreModulePresets\.Complete\);)$'), 'CreateScriptWithVersion(version);'),
        (re.compile(r'^(\s+)(Script script = new\(CoreModulePresets\.Complete\);)$'), 'CreateScriptWithVersion(version);'),
    ]

    # Track if we're in a test method that was modified
    modified_methods = set()

    while i < len(lines):
        line = lines[i]
        
        # Check for [Test] attribute without version args
        tm = test_marker.match(line)
        if tm:
            indent = tm.group(1)
            # Look ahead to see if there are already version Arguments
            look_ahead = ''
            for j in range(i+1, min(i+30, len(lines))):
                look_ahead += lines[j] + '\n'
                if method_marker.match(lines[j]):
                    break
            
            # If no LuaCompatibilityVersion in look_ahead, add version args
            if 'LuaCompatibilityVersion' not in look_ahead:
                result.append(line)
                # Adjust indentation of version_attrs to match the [Test] attribute
                adjusted_attrs = version_attrs.replace('        [', f'{indent}[')
                result.append(adjusted_attrs)
                # Now find the method signature and modify it
                i += 1
                while i < len(lines):
                    line = lines[i]
                    m = method_marker.match(line)
                    if m:
                        method_indent = m.group(1)
                        method_prefix = m.group(2)
                        method_name = m.group(3)
                        method_suffix = '(LuaCompatibilityVersion version)'
                        result.append(f'{method_indent}{method_prefix}{method_name}{method_suffix}')
                        modified_methods.add(method_name)
                        break
                    result.append(line)
                    i += 1
            else:
                result.append(line)
        else:
            # Check for CreateScript() calls - but only in test methods
            matched = False
            for pattern, replacement in create_script_patterns:
                m = pattern.match(line)
                if m:
                    # Check if we're inside a modified test method
                    method_name = None
                    for j in range(len(result)-1, max(0, len(result)-50), -1):
                        mm = re.match(r'^\s+public async Task (\w+)\(LuaCompatibilityVersion version\)$', result[j])
                        if mm:
                            method_name = mm.group(1)
                            break
                    
                    if method_name and method_name in modified_methods:
                        # Replace with version-aware script creation
                        indent = m.group(1)
                        result.append(f'{indent}Script script = {replacement}')
                        matched = True
                    break
            
            if not matched:
                result.append(line)
        i += 1

    # Write back
    with open(file_path, 'w') as f:
        f.write('\n'.join(result))

    return len(modified_methods)


def main():
    if len(sys.argv) < 2:
        print("Usage: python3 scripts/dev/add-version-coverage.py <file_path>")
        sys.exit(1)
    
    file_path = sys.argv[1]
    if not Path(file_path).exists():
        print(f"Error: File not found: {file_path}")
        sys.exit(1)
    
    count = add_version_coverage(file_path)
    print(f"Done! Modified {count} test methods in {Path(file_path).name}")


if __name__ == "__main__":
    main()
