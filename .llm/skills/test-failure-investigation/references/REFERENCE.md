# Test Failure Investigation Reference

## Comprehensive Fix Requirements

When you find the root cause, the fix must be **comprehensive**:

### For Production Bugs

1. Fix the root cause in production code
1. Add regression test(s) covering the fix
1. Create `.lua` fixture verifying behavior against reference Lua
1. Regenerate corpus: `python3 tools/LuaCorpusExtractor/lua_corpus_extractor_v2.py`
1. Verify fix across ALL applicable Lua versions
1. Document in `PLAN.md` if it's a spec compliance fix

### For Test Bugs

1. Verify correct behavior against reference Lua
1. Fix test expectation to match Lua spec
1. Update corresponding `.lua` fixture if exists
1. Ensure test runs on all applicable versions
1. Add version-specific tests if behavior differs by version

### For Isolation Bugs

1. Add missing isolation attributes
1. Review similar tests for same issue
1. Consider if production code needs thread-safety improvements
1. Document any new isolation patterns discovered

______________________________________________________________________

## 🔴 Cross-Platform Comparison Harness Failures

The Lua comparison harness runs fixtures against reference Lua on 3 platforms (macOS, Windows, Ubuntu) × 5 versions (5.1-5.5) in a CI lane that depends on `lint`, not the unit-test OS matrix. Treat comparison failures as independent Lua-spec signals even if a unit-test job also failed.

### Step 1: Categorize the Failure Pattern

| Pattern                               | Diagnosis                                             |
| ------------------------------------- | ----------------------------------------------------- |
| Fails on ALL platforms + ALL versions | Likely NovaSharp bug                                  |
| Fails on ONE platform only            | Platform-specific C library difference                |
| Fails on ONE version only             | Version-specific behavior (check `@lua-versions`)     |
| Fails on Windows only                 | Windows C library (MSVCRT) quirk                      |
| NovaSharp output is consistent        | Platform Lua varies; possibly `@novasharp-only: true` |

### Step 2: Check for Metadata Location Issues

**The #1 cause of unexpected failures**: Metadata placed incorrectly.

```bash
# Check if metadata is at the TOP of the file (no blank lines before it)
head -1 path/to/fixture.lua
# Should show: -- @lua-versions: ...

# If it shows a blank line or code, metadata is being SILENTLY IGNORED!
```

The harness parser **STOPS at the first non-comment line**. Metadata after a blank line is never read.

### Step 3: Check for Platform-Specific C Library Differences

Common platform differences that are NOT NovaSharp bugs:

| Function                          | Issue                                                          | Solution                |
| --------------------------------- | -------------------------------------------------------------- | ----------------------- |
| `os.date()` with POSIX specifiers | Windows doesn't support %C, %D, %F, %R, %T, %V, %u, %e, %n, %t | `@novasharp-only: true` |
| `tostring(0/0)`                   | Windows outputs `-nan(ind)`, others output `nan`               | Harness normalizes this |
| `math.pow`, `math.log10`, etc.    | Windows Lua may be built without compat flags                  | `@novasharp-only: true` |
| `__le` metamethod fallback        | Lua 5.4 Windows build may lack this                            | `@novasharp-only: true` |

### Step 4: Document the Reason

When marking tests as `@novasharp-only: true`, the fixture must document the
NovaSharp extension or platform C-library/spec implementation-defined behavior
that makes comparison against the local reference Lua inappropriate. Use a
plain comment next to the metadata; do not invent extra fixture metadata keys.

```lua
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- Windows strftime does not support %C; NovaSharp is POSIX-compliant on all platforms.

print(os.date("%C"))
```

### Step 5: Distinguish NovaSharp Bugs from Platform Quirks

**Key insight**: NovaSharp uses pure C# implementations that are often MORE correct (POSIX-compliant) than platform-specific reference Lua.

| Scenario                                        | Action                                                |
| ----------------------------------------------- | ----------------------------------------------------- |
| NovaSharp is MORE correct than platform Lua     | `@novasharp-only: true` with a plain explanatory note |
| NovaSharp differs from ALL platforms + versions | **Fix NovaSharp** - this is a real bug                |
| NovaSharp matches some platforms but not others | Check if NovaSharp matches the POSIX standard         |
| Reference Lua varies between platforms          | NovaSharp should be consistently correct              |

______________________________________________________________________

## Escalation Path

If you cannot determine root cause after thorough investigation:

1. **Document everything** — What you tried, what you observed
1. **Create minimal reproduction** — Smallest possible test case
1. **Check similar tests** — Are related tests also problematic?
1. **Review recent changes** — Did something in the area change recently?
1. **Ask for help** — With full documentation of investigation

Never leave a failing test unresolved. If investigation is incomplete, document current state and continue investigating.

______________________________________________________________________

## Common Root Causes Reference

| Symptom                       | Likely Cause                                       | Where to Look                    |
| ----------------------------- | -------------------------------------------------- | -------------------------------- |
| Wrong numeric result          | `LuaNumber` integer/float handling                 | `DataTypes/LuaNumber.cs`         |
| String comparison failure     | Encoding or escape sequences                       | `CoreLib/StringModule.cs`        |
| Table iteration order         | Table implementation                               | `DataTypes/Table.cs`             |
| Function call failure         | Argument marshalling                               | `Interop/`                       |
| Version-specific failure      | Feature flag or version check                      | `LuaCompatibilityVersion` usages |
| Memory/GC issues              | Pooling or closure captures                        | Resource management code         |
| Fixture skipped/wrong version | **Metadata location** (blank line before metadata) | Check first line of .lua file    |
| Platform-specific failure     | C library differences                              | See lua-comparison-harness skill |
| Windows-only failures         | MSVCRT strftime/NaN differences                    | Mark `@novasharp-only: true`     |

______________________________________________________________________

## Resources

- [codebase-navigation](../../codebase-navigation/SKILL.md) — Pipeline debugging
- [lua-spec-verification](../../lua-spec-verification/SKILL.md) — Verifying against reference Lua
- [tunit-test-writing](../../tunit-test-writing/SKILL.md) — Test patterns and isolation
- [docs/Testing.md](../../../../docs/Testing.md) — Testing documentation
