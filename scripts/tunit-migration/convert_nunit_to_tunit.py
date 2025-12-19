#!/usr/bin/env python3
"""
Convert NUnit assertions to TUnit assertions in C# test files.

This script handles the migration from NUnit patterns to TUnit patterns:
- Assert.That(x, Is.EqualTo(y)) -> await Assert.That(x).IsEqualTo(y).ConfigureAwait(false)
- Assert.That(x, Is.Not.Null) -> await Assert.That(x).IsNotNull().ConfigureAwait(false)
- Assert.Throws<T>(() => ...) -> await Assert.That(() => ...).ThrowsException().OfType<T>().ConfigureAwait(false)
- Assert.Multiple(() => { ... }) -> Individual assertions
- etc.

Usage:
    python3 scripts/tunit-migration/convert_nunit_to_tunit.py <file_path>
    python3 scripts/tunit-migration/convert_nunit_to_tunit.py --all  # Process all TUnit test files
    python3 scripts/tunit-migration/convert_nunit_to_tunit.py --dry-run <file_path>  # Show changes without writing
"""

import argparse
import re
import sys
from pathlib import Path


def convert_assert_throws(line: str, indent: str) -> str:
    """
    Convert Assert.Throws<ExceptionType>(() => expression) patterns.
    
    Patterns:
    - ExceptionType var = Assert.Throws<ExceptionType>(() => expr);
    - Assert.Throws<ExceptionType>(() => expr);
    - _ = Assert.Throws<ExceptionType>(() => expr);
    """
    # Pattern 1: Type exception = Assert.Throws<Type>(() => ...);
    match = re.match(
        r'^(\s*)(\w+)\s+(\w+)\s*=\s*Assert\.Throws<(\w+)>\(\s*\(\)\s*=>\s*$',
        line
    )
    if match:
        var_type = match.group(2)
        var_name = match.group(3)
        exc_type = match.group(4)
        return f"{indent}{var_type} {var_name} = await Assert.That(() =>\n"
    
    # Pattern 2: Type exception = Assert.Throws<Type>(() => { ... }); on single line
    match = re.match(
        r'^(\s*)(\w+)\s+(\w+)\s*=\s*Assert\.Throws<(\w+)>\(\s*\(\)\s*=>\s*(.+)\s*\);\s*$',
        line
    )
    if match:
        var_type = match.group(2)
        var_name = match.group(3)
        exc_type = match.group(4)
        expr = match.group(5).strip()
        return f"{indent}{var_type} {var_name} = await Assert.That(() => {expr}).ThrowsException().OfType<{exc_type}>().ConfigureAwait(false);\n"
    
    # Pattern 3: Assert.Throws<Type>(() => expr); standalone
    match = re.match(
        r'^(\s*)Assert\.Throws<(\w+)>\(\s*\(\)\s*=>\s*(.+)\s*\);\s*$',
        line
    )
    if match:
        exc_type = match.group(2)
        expr = match.group(3).strip()
        return f"{indent}await Assert.That(() => {expr}).ThrowsException().OfType<{exc_type}>().ConfigureAwait(false);\n"
    
    # Pattern 4: _ = Assert.Throws<Type>(() => ...);
    match = re.match(
        r'^(\s*)_\s*=\s*Assert\.Throws<(\w+)>\(\s*\(\)\s*=>\s*(.+)\s*\);\s*$',
        line
    )
    if match:
        exc_type = match.group(2)
        expr = match.group(3).strip()
        return f"{indent}await Assert.That(() => {expr}).ThrowsException().OfType<{exc_type}>().ConfigureAwait(false);\n"
    
    return None


def convert_assert_that_simple(line: str) -> str:
    """
    Convert simple Assert.That(x, Is.Y) patterns to TUnit style.
    """
    indent_match = re.match(r'^(\s*)', line)
    indent = indent_match.group(1) if indent_match else ""
    
    # Is.EqualTo patterns
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.EqualTo\((.+)\)\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        expected = match.group(3).strip()
        return f"{indent}await Assert.That({actual}).IsEqualTo({expected}).ConfigureAwait(false);\n"
    
    # Is.EqualTo with message
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.EqualTo\((.+)\),\s*(.+)\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        expected = match.group(3).strip()
        # TUnit doesn't have .WithMessage() - just use comment or drop
        return f"{indent}await Assert.That({actual}).IsEqualTo({expected}).ConfigureAwait(false);\n"
    
    # Is.Not.Null
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.Not\.Null\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        return f"{indent}await Assert.That({actual}).IsNotNull().ConfigureAwait(false);\n"
    
    # Is.Null
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.Null\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        return f"{indent}await Assert.That({actual}).IsNull().ConfigureAwait(false);\n"
    
    # Is.True
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.True\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        return f"{indent}await Assert.That({actual}).IsTrue().ConfigureAwait(false);\n"
    
    # Is.False
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.False(,\s*.+)?\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        return f"{indent}await Assert.That({actual}).IsFalse().ConfigureAwait(false);\n"
    
    # Is.InstanceOf<T>()
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.InstanceOf<(.+)>\(\)\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        expected_type = match.group(3).strip()
        return f"{indent}await Assert.That({actual}).IsTypeOf<{expected_type}>().ConfigureAwait(false);\n"
    
    # Is.Empty
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.Empty\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        return f"{indent}await Assert.That({actual}).IsEmpty().ConfigureAwait(false);\n"
    
    # Is.Not.Empty
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.Not\.Empty\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        return f"{indent}await Assert.That({actual}).IsNotEmpty().ConfigureAwait(false);\n"
    
    # Is.GreaterThan
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.GreaterThan\((.+)\)\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        expected = match.group(3).strip()
        return f"{indent}await Assert.That({actual}).IsGreaterThan({expected}).ConfigureAwait(false);\n"
    
    # Is.LessThan
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.LessThan\((.+)\)\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        expected = match.group(3).strip()
        return f"{indent}await Assert.That({actual}).IsLessThan({expected}).ConfigureAwait(false);\n"
    
    # Is.GreaterThanOrEqualTo
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.GreaterThanOrEqualTo\((.+)\)\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        expected = match.group(3).strip()
        return f"{indent}await Assert.That({actual}).IsGreaterThanOrEqualTo({expected}).ConfigureAwait(false);\n"
    
    # Is.LessThanOrEqualTo
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.LessThanOrEqualTo\((.+)\)\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        expected = match.group(3).strip()
        return f"{indent}await Assert.That({actual}).IsLessThanOrEqualTo({expected}).ConfigureAwait(false);\n"
    
    # Is.SameAs
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.SameAs\((.+)\)\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        expected = match.group(3).strip()
        return f"{indent}await Assert.That({actual}).IsSameReferenceAs({expected}).ConfigureAwait(false);\n"
    
    # Is.Not.SameAs
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Is\.Not\.SameAs\((.+)\)\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        expected = match.group(3).strip()
        return f"{indent}await Assert.That({actual}).IsNotSameReferenceAs({expected}).ConfigureAwait(false);\n"
    
    # Has.Count.EqualTo
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Has\.Count\.EqualTo\((.+)\)\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        expected = match.group(3).strip()
        return f"{indent}await Assert.That({actual}).HasCount().EqualTo({expected}).ConfigureAwait(false);\n"
    
    # Contains.Item
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Contains\.Item\((.+)\)\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        expected = match.group(3).strip()
        return f"{indent}await Assert.That({actual}).Contains({expected}).ConfigureAwait(false);\n"
    
    # Does.Contain
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Does\.Contain\((.+)\)\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        expected = match.group(3).strip()
        return f"{indent}await Assert.That({actual}).Contains({expected}).ConfigureAwait(false);\n"
    
    # Does.Not.Contain
    match = re.match(
        r'^(\s*)Assert\.That\((.+),\s*Does\.Not\.Contain\((.+)\)\);\s*$',
        line
    )
    if match:
        actual = match.group(2).strip()
        expected = match.group(3).strip()
        return f"{indent}await Assert.That({actual}).DoesNotContain({expected}).ConfigureAwait(false);\n"
    
    return None


def make_method_async(content: str) -> str:
    """
    Make void test methods async Task if they contain await.
    """
    # Find methods that need to be async
    # Pattern: public void MethodName()
    def replace_method(match):
        indent = match.group(1)
        name = match.group(2)
        return f"{indent}public async Task {name}()"
    
    # Only replace if method contains await
    lines = content.split('\n')
    result = []
    i = 0
    while i < len(lines):
        line = lines[i]
        # Check for method signature
        method_match = re.match(r'^(\s*)public\s+void\s+(\w+)\s*\(\s*\)\s*$', line)
        if method_match:
            # Look ahead to see if method body contains 'await'
            brace_count = 0
            has_await = False
            method_lines = [line]
            j = i + 1
            found_open = False
            while j < len(lines):
                method_lines.append(lines[j])
                if '{' in lines[j]:
                    brace_count += lines[j].count('{')
                    found_open = True
                if '}' in lines[j]:
                    brace_count -= lines[j].count('}')
                if 'await ' in lines[j]:
                    has_await = True
                if found_open and brace_count == 0:
                    break
                j += 1
            
            if has_await:
                # Convert to async Task
                result.append(re.sub(
                    r'^(\s*)public\s+void\s+(\w+)\s*\(\s*\)',
                    r'\1public async Task \2()',
                    line
                ))
            else:
                result.append(line)
            i += 1
        else:
            result.append(line)
            i += 1
    
    return '\n'.join(result)


def remove_nunit_using(content: str) -> str:
    """Remove the 'using NUnit.Framework;' line."""
    lines = content.split('\n')
    result = [line for line in lines if not re.match(r'^\s*using\s+NUnit\.Framework\s*;\s*$', line)]
    return '\n'.join(result)


def add_system_threading_tasks_using(content: str) -> str:
    """Add 'using System.Threading.Tasks;' if not present and async is used."""
    if 'async Task' in content and 'using System.Threading.Tasks;' not in content:
        # Find the last using statement and add after it
        lines = content.split('\n')
        last_using_idx = -1
        for i, line in enumerate(lines):
            if re.match(r'^\s*using\s+', line):
                last_using_idx = i
        
        if last_using_idx >= 0:
            lines.insert(last_using_idx + 1, '    using System.Threading.Tasks;')
        
        return '\n'.join(lines)
    return content


def process_file(file_path: Path, dry_run: bool = False) -> tuple[bool, str]:
    """
    Process a single file and convert NUnit assertions to TUnit.
    Returns (changed, new_content).
    """
    content = file_path.read_text(encoding='utf-8')
    original = content
    
    # Process line by line for simple conversions
    lines = content.split('\n')
    new_lines = []
    
    in_multiline_throws = False
    multiline_buffer = []
    throws_var_type = None
    throws_var_name = None
    throws_exc_type = None
    
    for i, line in enumerate(lines):
        indent_match = re.match(r'^(\s*)', line)
        indent = indent_match.group(1) if indent_match else ""
        
        # Handle multiline Assert.Throws
        if in_multiline_throws:
            multiline_buffer.append(line)
            if ').' in line or ');' in line:
                # End of multiline throws - check if it ends with .ConfigureAwait or not
                full_expr = '\n'.join(multiline_buffer)
                
                # Check if it's a closing with additional assertions like .With.Message
                if '.ConfigureAwait(false);' not in full_expr:
                    # Find the expression content
                    expr_match = re.search(r'\(\)\s*=>\s*\n?\s*\{?([\s\S]*?)\}?\s*\)', full_expr, re.MULTILINE)
                    if expr_match:
                        expr = expr_match.group(1).strip()
                        # Reconstruct as TUnit
                        if throws_var_name:
                            new_lines.append(f"{indent}{throws_var_type} {throws_var_name} = await Assert.That(() =>\n")
                            for buf_line in multiline_buffer[:-1]:
                                new_lines.append(buf_line)
                            # Replace the closing
                            closing = multiline_buffer[-1]
                            closing = re.sub(
                                r'\)\s*;',
                                f').ThrowsException().OfType<{throws_exc_type}>().ConfigureAwait(false);',
                                closing
                            )
                            new_lines.append(closing)
                        else:
                            new_lines.extend(multiline_buffer)
                    else:
                        new_lines.extend(multiline_buffer)
                else:
                    new_lines.extend(multiline_buffer)
                
                in_multiline_throws = False
                multiline_buffer = []
                throws_var_type = None
                throws_var_name = None
                throws_exc_type = None
                continue
            continue
        
        # Check for start of multiline Assert.Throws
        match = re.match(
            r'^(\s*)(\w+)\s+(\w+)\s*=\s*Assert\.Throws<(\w+)>\(\s*\(\)\s*=>\s*$',
            line
        )
        if match:
            in_multiline_throws = True
            throws_var_type = match.group(2)
            throws_var_name = match.group(3)
            throws_exc_type = match.group(4)
            # Start the TUnit pattern
            new_lines.append(f"{indent}{throws_var_type} {throws_var_name} = await Assert.That(() =>\n")
            continue
        
        # Try simple Assert.Throws conversion (single line)
        throws_result = convert_assert_throws(line, indent)
        if throws_result:
            new_lines.append(throws_result.rstrip('\n'))
            continue
        
        # Try simple Assert.That conversions
        that_result = convert_assert_that_simple(line)
        if that_result:
            new_lines.append(that_result.rstrip('\n'))
            continue
        
        new_lines.append(line)
    
    content = '\n'.join(new_lines)
    
    # Make methods async if they contain await
    content = make_method_async(content)
    
    # Remove NUnit using
    content = remove_nunit_using(content)
    
    # Add System.Threading.Tasks using if needed
    content = add_system_threading_tasks_using(content)
    
    changed = content != original
    
    if changed and not dry_run:
        file_path.write_text(content, encoding='utf-8')
    
    return changed, content


def find_test_files(base_path: Path) -> list[Path]:
    """Find all TUnit test files."""
    return list(base_path.glob('**/*TUnitTests.cs'))


def main():
    parser = argparse.ArgumentParser(description='Convert NUnit assertions to TUnit')
    parser.add_argument('file', nargs='?', help='File to process')
    parser.add_argument('--all', action='store_true', help='Process all TUnit test files')
    parser.add_argument('--dry-run', action='store_true', help='Show changes without writing')
    parser.add_argument('--verbose', '-v', action='store_true', help='Verbose output')
    
    args = parser.parse_args()
    
    if args.all:
        base_path = Path(__file__).parent.parent.parent / 'src' / 'tests' / 'WallstopStudios.NovaSharp.Interpreter.Tests.TUnit'
        files = find_test_files(base_path)
        print(f"Found {len(files)} test files")
        
        changed_count = 0
        for f in files:
            changed, _ = process_file(f, args.dry_run)
            if changed:
                changed_count += 1
                if args.verbose:
                    print(f"  Changed: {f.name}")
        
        print(f"Modified {changed_count} files" + (" (dry run)" if args.dry_run else ""))
    elif args.file:
        file_path = Path(args.file)
        if not file_path.exists():
            print(f"File not found: {file_path}")
            sys.exit(1)
        
        changed, content = process_file(file_path, args.dry_run)
        if args.dry_run:
            print(content)
        elif changed:
            print(f"Modified: {file_path}")
        else:
            print(f"No changes: {file_path}")
    else:
        parser.print_help()
        sys.exit(1)


if __name__ == '__main__':
    main()
