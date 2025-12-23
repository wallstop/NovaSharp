# TUnit Migration Scripts

Scripts to assist with migrating test files from NUnit to TUnit.

## Scripts

### `convert_nunit_to_tunit.py`

Full-featured NUnit to TUnit assertion converter with comprehensive pattern support.

**Features:**

- Converts `Assert.That(x, Is.EqualTo(y))` to `await Assert.That(x).IsEqualTo(y).ConfigureAwait(false)`
- Converts `Assert.That(x, Is.Not.Null)` to `await Assert.That(x).IsNotNull().ConfigureAwait(false)`
- Converts `Assert.Throws<T>(() => ...)` to `await Assert.That(() => ...).ThrowsException().OfType<T>().ConfigureAwait(false)`
- Handles multiline `Assert.Throws` patterns
- Converts `void` test methods to `async Task` when they contain `await`
- Removes `using NUnit.Framework;` and adds `using System.Threading.Tasks;`

**Usage:**

```bash
# Convert a single file
python3 scripts/tunit-migration/convert_nunit_to_tunit.py <file_path>

# Process all TUnit test files
python3 scripts/tunit-migration/convert_nunit_to_tunit.py --all

# Dry run (show changes without writing)
python3 scripts/tunit-migration/convert_nunit_to_tunit.py --dry-run <file_path>

# Verbose output
python3 scripts/tunit-migration/convert_nunit_to_tunit.py --all --verbose
```

### `convert_simple.py`

Lightweight NUnit to TUnit converter for straightforward assertion patterns.

**Features:**

- Basic `Assert.That` pattern conversions (EqualTo, True, False, Null, Not.Null, InstanceOf)
- Simple `Assert.Throws` single-line conversions
- Converts `void` methods to `async Task` when needed
- Handles `Assert.Fail()` conversion

**Usage:**

```bash
python3 scripts/tunit-migration/convert_simple.py <file_path>
python3 scripts/tunit-migration/convert_simple.py <file_path> --dry-run
```

### `convert_event_member.py`

Specialized converter for complex assertion patterns including exception message validation.

**Features:**

- All basic `Assert.That` conversions
- Handles `Assert.Multiple(() => { ... })` blocks
- Converts `Throws.TypeOf<T>().With.Message.Contains(...)` patterns
- Converts `Throws.ArgumentException.With.Message.Contains(...)` patterns

**Usage:**

```bash
python3 scripts/tunit-migration/convert_event_member.py <file_path>
```

## Notes

- These scripts are migration aids, not perfect converters. Manual review is recommended after conversion.
- Some complex patterns may require manual adjustment.
- Always run tests after conversion to verify correctness.
