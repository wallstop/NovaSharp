#!/usr/bin/env python3
"""
Convert NUnit assertions to TUnit assertions in C# test files.

This script handles the migration from NUnit patterns to TUnit patterns:
- Assert.That(x, Is.EqualTo(y)) -> await Assert.That(x).IsEqualTo(y).ConfigureAwait(false)
- Assert.That(x, Is.Not.Null) -> await Assert.That(x).IsNotNull().ConfigureAwait(false)
- Assert.Throws<T>(() => ...) -> await Assert.That(() => ...).Throws<T>().ConfigureAwait(false)
- Preserves void for methods without await
"""

import re
import sys
from pathlib import Path


def convert_file(file_path: Path) -> str:
    """Convert a file from NUnit to TUnit assertions."""
    content = file_path.read_text(encoding='utf-8')
    
    # Remove NUnit using and add System.Threading.Tasks
    content = re.sub(r'using\s+NUnit\.Framework;\s*\n', 'using System.Threading.Tasks;\n', content)
    
    # Track which method bodies contain await (after conversion)
    # We'll do the conversion first, then adjust method signatures
    
    # Convert Assert.That(x, Is.EqualTo(y)); - simple case
    content = re.sub(
        r'Assert\.That\(([^,]+),\s*Is\.EqualTo\(([^)]+)\)\);',
        r'await Assert.That(\1).IsEqualTo(\2).ConfigureAwait(false);',
        content
    )
    
    # Convert Assert.That(x, Is.EqualTo(y), "message"); - with message
    content = re.sub(
        r'Assert\.That\(([^,]+),\s*Is\.EqualTo\(([^)]+)\),\s*"[^"]*"\);',
        r'await Assert.That(\1).IsEqualTo(\2).ConfigureAwait(false);',
        content
    )
    
    # Convert Assert.That(x, Is.True);
    content = re.sub(
        r'Assert\.That\(([^,]+),\s*Is\.True\);',
        r'await Assert.That(\1).IsTrue().ConfigureAwait(false);',
        content
    )
    
    # Convert Assert.That(x, Is.False); or with message
    content = re.sub(
        r'Assert\.That\(([^,]+),\s*Is\.False(?:,\s*[^)]+)?\);',
        r'await Assert.That(\1).IsFalse().ConfigureAwait(false);',
        content
    )
    
    # Convert Assert.That(x, Is.Not.Null);
    content = re.sub(
        r'Assert\.That\(([^,]+),\s*Is\.Not\.Null\);',
        r'await Assert.That(\1).IsNotNull().ConfigureAwait(false);',
        content
    )
    
    # Convert Assert.That(x, Is.Null);
    content = re.sub(
        r'Assert\.That\(([^,]+),\s*Is\.Null\);',
        r'await Assert.That(\1).IsNull().ConfigureAwait(false);',
        content
    )
    
    # Convert Assert.That(x, Is.InstanceOf<T>());
    content = re.sub(
        r'Assert\.That\(([^,]+),\s*Is\.InstanceOf<([^>]+)>\(\)\);',
        r'await Assert.That(\1).IsTypeOf<\2>().ConfigureAwait(false);',
        content
    )
    
    # Convert simple Assert.Throws<T>(() => expr); (single line)
    content = re.sub(
        r'Assert\.Throws<([^>]+)>\(\s*\(\)\s*=>\s*([^)]+\([^)]*\))\);',
        r'await Assert.That(() => \2).Throws<\1>().ConfigureAwait(false);',
        content
    )
    
    # Convert Assert.Fail(); -> Assert.Fail("reason");
    content = re.sub(
        r'Assert\.Fail\(\);',
        r'Assert.Fail("Forced failure");',
        content
    )
    
    # Now handle method signatures - find methods with await and make them async Task
    # Parse method by method
    lines = content.split('\n')
    result_lines = []
    i = 0
    
    while i < len(lines):
        line = lines[i]
        
        # Check for test method signature: public void MethodName()
        match = re.match(r'^(\s*)public\s+void\s+(\w+)\s*\(\s*\)\s*$', line)
        if match:
            indent = match.group(1)
            method_name = match.group(2)
            
            # Collect entire method body
            method_lines = [line]
            brace_count = 0
            j = i + 1
            found_open = False
            
            while j < len(lines):
                method_lines.append(lines[j])
                if '{' in lines[j]:
                    brace_count += lines[j].count('{')
                    found_open = True
                if '}' in lines[j]:
                    brace_count -= lines[j].count('}')
                if found_open and brace_count == 0:
                    break
                j += 1
            
            # Check if method body contains 'await '
            method_body = '\n'.join(method_lines)
            has_await = 'await ' in method_body
            
            if has_await:
                # Convert to async Task
                result_lines.append(f'{indent}public async Task {method_name}()')
            else:
                result_lines.append(line)
            
            # Add remaining method lines
            for k in range(1, len(method_lines)):
                result_lines.append(method_lines[k])
            
            i = j + 1
        else:
            result_lines.append(line)
            i += 1
    
    return '\n'.join(result_lines)


def main():
    if len(sys.argv) < 2:
        print("Usage: python convert_simple.py <file_path>")
        sys.exit(1)
    
    file_path = Path(sys.argv[1])
    if not file_path.exists():
        print(f"File not found: {file_path}")
        sys.exit(1)
    
    result = convert_file(file_path)
    
    if len(sys.argv) > 2 and sys.argv[2] == '--dry-run':
        print(result)
    else:
        file_path.write_text(result, encoding='utf-8')
        print(f"Converted: {file_path}")


if __name__ == '__main__':
    main()
