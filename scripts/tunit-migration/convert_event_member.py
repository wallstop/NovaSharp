#!/usr/bin/env python3
"""
Convert EventMemberDescriptorTUnitTests.cs from NUnit to TUnit assertions.
This handles:
- Assert.That(x, Is.EqualTo(y)) -> await Assert.That(x).IsEqualTo(y).ConfigureAwait(false)
- Assert.That(x, Is.Not.Null) -> await Assert.That(x).IsNotNull().ConfigureAwait(false)
- Assert.That(x, Is.Null) -> await Assert.That(x).IsNull().ConfigureAwait(false)
- Assert.That(x, Is.True) -> await Assert.That(x).IsTrue().ConfigureAwait(false)
- Assert.That(x, Is.False) -> await Assert.That(x).IsFalse().ConfigureAwait(false)
- Assert.That(x, Is.InstanceOf<T>()) -> await Assert.That(x).IsTypeOf<T>().ConfigureAwait(false)
- Assert.Multiple(() => { ... }) -> Individual assertions
- Assert.That(() => ..., Throws.TypeOf<T>()...) -> Complex exception assertions
"""

import re
import sys
from pathlib import Path


def convert_event_member_file(file_path: Path) -> str:
    content = file_path.read_text(encoding='utf-8')
    
    # Step 1: Remove NUnit using and add System.Threading.Tasks
    content = re.sub(r'using\s+NUnit\.Framework;\s*\n', 'using System.Threading.Tasks;\n', content)
    
    # Step 2: Convert simple Assert.That patterns (single line, no nested parens in Is.EqualTo)
    # Is.EqualTo
    content = re.sub(
        r'Assert\.That\(([^,\n]+),\s*Is\.EqualTo\(([^)]+)\)\);',
        r'await Assert.That(\1).IsEqualTo(\2).ConfigureAwait(false);',
        content
    )
    
    # Is.EqualTo with message
    content = re.sub(
        r'Assert\.That\(([^,\n]+),\s*Is\.EqualTo\(([^)]+)\),\s*"[^"]*"\);',
        r'await Assert.That(\1).IsEqualTo(\2).ConfigureAwait(false);',
        content
    )
    
    # Is.Not.Null
    content = re.sub(
        r'Assert\.That\(([^,\n]+),\s*Is\.Not\.Null\);',
        r'await Assert.That(\1).IsNotNull().ConfigureAwait(false);',
        content
    )
    
    # Is.Null
    content = re.sub(
        r'Assert\.That\(([^,\n]+),\s*Is\.Null\);',
        r'await Assert.That(\1).IsNull().ConfigureAwait(false);',
        content
    )
    
    # Is.True
    content = re.sub(
        r'Assert\.That\(([^,\n]+),\s*Is\.True\);',
        r'await Assert.That(\1).IsTrue().ConfigureAwait(false);',
        content
    )
    
    # Is.False
    content = re.sub(
        r'Assert\.That\(([^,\n]+),\s*Is\.False\);',
        r'await Assert.That(\1).IsFalse().ConfigureAwait(false);',
        content
    )
    
    # Is.InstanceOf<T>()
    content = re.sub(
        r'Assert\.That\(([^,\n]+),\s*Is\.InstanceOf<([^>]+)>\(\)\);',
        r'await Assert.That(\1).IsTypeOf<\2>().ConfigureAwait(false);',
        content
    )
    
    # Step 3: Remove Assert.Multiple(() => { ... }) wrapper but keep contents
    # This is a simplified approach - just removes the wrapper
    content = re.sub(
        r'Assert\.Multiple\(\(\)\s*=>\s*\n\s*\{',
        '// Note: Assert.Multiple removed - converted to sequential assertions\n            {',
        content
    )
    
    # Step 4: Handle Throws patterns within Assert.That
    # These are complex NUnit constraint patterns like:
    # Assert.That(() => expr, Throws.TypeOf<T>().With.Message.Contains("..."))
    # Convert to: var exception = Assert.Throws<T>(() => expr);
    #             await Assert.That(exception.Message).Contains("...").ConfigureAwait(false);
    
    # Pattern for simple Throws.TypeOf
    content = re.sub(
        r'Assert\.That\(\s*\n?\s*\(\)\s*=>\s*\n?\s*([^,]+),\s*\n?\s*Throws\.TypeOf<([^>]+)>\(\)\s*\n?\s*\);',
        r'Assert.Throws<\2>(() => \1);',
        content,
        flags=re.MULTILINE | re.DOTALL
    )
    
    # Pattern for Throws.ArgumentException.With.Message.Contains
    # This needs to be converted to catch and check
    def convert_throws_with_message(match):
        expr = match.group(1).strip()
        exc_type = match.group(2).strip()
        message_contains = match.group(3).strip()
        return f'''{{
                {exc_type} exception = Assert.Throws<{exc_type}>(() => {expr});
                await Assert.That(exception.Message).Contains({message_contains}).ConfigureAwait(false);
            }}'''
    
    content = re.sub(
        r'Assert\.That\(\s*\(\)\s*=>\s*([^,]+),\s*Throws\.([A-Za-z]+)\.With\.Message\.Contains\(([^)]+)\)\s*\);',
        convert_throws_with_message,
        content,
        flags=re.MULTILINE | re.DOTALL
    )
    
    # Step 5: Handle Throws.TypeOf<T>().With.Message.Contains patterns
    def convert_throws_typeof_with_message(match):
        expr = match.group(1).strip()
        exc_type = match.group(2).strip()
        message_contains = match.group(3).strip()
        return f'''{{
                {exc_type} exception = Assert.Throws<{exc_type}>(() => {expr});
                await Assert.That(exception.Message).Contains({message_contains}).ConfigureAwait(false);
            }}'''
    
    content = re.sub(
        r'Assert\.That\(\s*\n?\s*\(\)\s*=>\s*\n?\s*([^,\n]+(?:\n[^,\n]+)*),\s*\n?\s*Throws\s*\n?\s*\.TypeOf<([^>]+)>\(\)\s*\n?\s*\.With\.Message\.Contains\(([^)]+)\)\s*\n?\s*\);',
        convert_throws_typeof_with_message,
        content,
        flags=re.MULTILINE | re.DOTALL
    )
    
    # Step 6: Convert void methods to async Task if they contain await
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
        print("Usage: python convert_event_member.py <file_path>")
        sys.exit(1)
    
    file_path = Path(sys.argv[1])
    if not file_path.exists():
        print(f"File not found: {file_path}")
        sys.exit(1)
    
    result = convert_event_member_file(file_path)
    file_path.write_text(result, encoding='utf-8')
    print(f"Converted: {file_path}")


if __name__ == '__main__':
    main()
