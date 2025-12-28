# Skill: Documentation and Changelog Management

**When to use**: After implementing any new feature, fixing a bug, or making any user-facing change.

**Related Skills**: [tunit-test-writing](tunit-test-writing.md) (comprehensive testing), [lua-fixture-creation](lua-fixture-creation.md) (test fixtures)

______________________________________________________________________

## 🔴 Critical: Documentation is NOT Optional

**Every feature and bugfix requires documentation updates.** Code without documentation is incomplete. Documentation includes:

1. **Code comments** — Non-obvious design rationale (WHY, not WHAT)
1. **XML documentation** — All public API members
1. **Markdown docs** — User guides, API references in `docs/`
1. **Code samples** — Working, tested examples
1. **CHANGELOG.md** — User-facing changes in keepachangelog format

______________________________________________________________________

## 🔴 Changelog Requirements (Keep a Changelog 1.1.0)

NovaSharp follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) format.

### Changelog Location

`CHANGELOG.md` in the repository root.

### Required Structure

```markdown
# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- New features go here

### Changed
- Changes to existing functionality

### Deprecated
- Features to be removed in upcoming releases

### Removed
- Features removed in this release

### Fixed
- Bug fixes

### Security
- Vulnerability fixes

## [1.0.0] - 2024-01-15

### Added
- Initial release features...

[Unreleased]: https://github.com/user/repo/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/user/repo/releases/tag/v1.0.0
```

### Category Definitions

| Category       | When to Use                                           | Example                                        |
| -------------- | ----------------------------------------------------- | ---------------------------------------------- |
| **Added**      | New features, new API methods, new functionality      | "Add `math.type()` support for Lua 5.3+"       |
| **Changed**    | Changes to existing behavior (non-breaking if minor)  | "Improve error messages for invalid arguments" |
| **Deprecated** | Features marked for removal (still works, but warned) | "Deprecate `Script.LoadFile()` in favor of..." |
| **Removed**    | Features that no longer exist                         | "Remove legacy `MoonSharp` namespace aliases"  |
| **Fixed**      | Bug fixes                                             | "Fix `string.format` crash with nil arguments" |
| **Security**   | Vulnerability patches                                 | "Fix sandbox escape via `debug` library"       |

### Entry Format

Each entry should be:

- **Concise** — One line if possible
- **User-focused** — Describe impact, not implementation details
- **Specific** — Include affected API/feature name
- **Linked** — Reference issues/PRs when applicable

```markdown
### Fixed
- Fix `math.floor()` returning incorrect results for negative numbers near zero (#123)
- Fix table iteration order not matching reference Lua 5.4 behavior

### Added
- Add `utf8` library support for Lua 5.3+ compatibility
- Add `Script.DoStringAsync()` for non-blocking script execution
```

### When to Update Changelog

| Change Type                       | Update Changelog? | Category   |
| --------------------------------- | ----------------- | ---------- |
| New public API method             | ✅ YES            | Added      |
| Bug fix affecting users           | ✅ YES            | Fixed      |
| Performance improvement           | ✅ YES            | Changed    |
| Internal refactor (no behavior Δ) | ❌ NO             | —          |
| Test additions only               | ❌ NO             | —          |
| Documentation fixes only          | ❌ NO             | —          |
| Breaking API change               | ✅ YES            | Changed    |
| Deprecation warning added         | ✅ YES            | Deprecated |
| Security vulnerability fix        | ✅ YES            | Security   |

______________________________________________________________________

## 🔴 Documentation Checklist

### For New Features

- [ ] XML docs on all public types, methods, properties, events
- [ ] Code sample showing basic usage
- [ ] Code sample showing edge cases (if applicable)
- [ ] Update relevant `docs/*.md` files
- [ ] Add entry to CHANGELOG.md under `[Unreleased]` → `Added`
- [ ] Note if behavior is NEW (not present in previous versions)

### For Bug Fixes

- [ ] Update any affected documentation/samples
- [ ] Add entry to CHANGELOG.md under `[Unreleased]` → `Fixed`
- [ ] Note if behavior change may affect existing users

### For Breaking Changes

- [ ] Document migration path in detail
- [ ] Add entry to CHANGELOG.md under `[Unreleased]` → `Changed` or `Removed`
- [ ] Mark as BREAKING CHANGE explicitly
- [ ] Consider deprecation period first

______________________________________________________________________

## 🔴 XML Documentation Standards

### Required for All Public Members

```csharp
/// <summary>
/// Executes a Lua script from a string.
/// </summary>
/// <param name="code">The Lua source code to execute.</param>
/// <returns>The result of script execution, or <see cref="DynValue.Nil"/> if the script returns nothing.</returns>
/// <exception cref="SyntaxErrorException">Thrown when the Lua code contains syntax errors.</exception>
/// <exception cref="ScriptRuntimeException">Thrown when a runtime error occurs during execution.</exception>
/// <example>
/// <code>
/// Script script = new Script();
/// DynValue result = script.DoString("return 1 + 2");
/// // result.Number is 3
/// </code>
/// </example>
public DynValue DoString(string code)
```

### XML Doc Requirements

| Element       | Required For                  | Content                                    |
| ------------- | ----------------------------- | ------------------------------------------ |
| `<summary>`   | ALL public members            | Clear, concise description of purpose      |
| `<param>`     | All parameters                | What the parameter represents              |
| `<returns>`   | Non-void methods              | What is returned and when                  |
| `<exception>` | Methods that throw            | Which exceptions and under what conditions |
| `<example>`   | Complex or non-obvious APIs   | Working code sample                        |
| `<remarks>`   | When additional context helps | Implementation notes, gotchas              |
| `<seealso>`   | Related APIs                  | Links to related types/methods             |

### Quality Standards

- **Accuracy** — Documentation MUST match actual behavior
- **Completeness** — All edge cases documented
- **Clarity** — A developer unfamiliar with the codebase should understand
- **Examples** — Must compile and run correctly
- **Up-to-date** — Update when behavior changes

______________________________________________________________________

## 🔴 Code Sample Standards

### Every Code Sample MUST

1. **Compile** — Syntactically correct, no missing usings
1. **Run** — Execute without errors
1. **Be tested** — Have a corresponding test that validates it works
1. **Be complete** — Include all necessary setup code
1. **Be minimal** — Show only what's needed, no distractions

### Sample Template

```csharp
// ✅ GOOD: Complete, minimal, correct
Script script = new Script();
DynValue result = script.DoString("return math.floor(3.7)");
Console.WriteLine(result.Number);  // Output: 3
```

```csharp
// ❌ BAD: Incomplete (missing Script creation)
DynValue result = script.DoString("return math.floor(3.7)");  // Where does 'script' come from?
```

```csharp
// ❌ BAD: Incorrect (wrong expected output)
Script script = new Script();
DynValue result = script.DoString("return math.floor(-0.5)");
Console.WriteLine(result.Number);  // Output: 0  ← WRONG! Should be -1
```

### Sample Types to Include

| Scenario         | When to Include                      |
| ---------------- | ------------------------------------ |
| Basic usage      | Always                               |
| Edge cases       | When behavior is non-obvious         |
| Error handling   | When exceptions can occur            |
| Version-specific | When behavior differs by Lua version |
| Integration      | When combining with other features   |

______________________________________________________________________

## 🔴 External Link Best Practices

External URLs can break over time. Follow these guidelines to minimize broken links:

### Prefer Stable/Canonical URLs

| ❌ Avoid                                | ✅ Use Instead                              |
| --------------------------------------- | ------------------------------------------- |
| `docs.microsoft.com/...`                | `learn.microsoft.com/...`                   |
| Deep links to specific sections         | Landing pages when content moves frequently |
| URLs with version numbers in path       | Version-agnostic URLs when available        |
| Short URLs / redirects (bit.ly, aka.ms) | Canonical full URLs                         |

### Verify Links Before Committing

When adding external links to markdown files:

```bash
# Check specific file(s)
python3 scripts/ci/check_markdown_links.py --files path/to/your.md

# Check all markdown files
python3 scripts/ci/check_markdown_links.py --all
```

### For Frequently-Changing Documentation

When linking to external documentation that moves frequently:

1. **Prefer official landing pages** over deep links (e.g., link to the docs homepage, not a specific subsection)
1. **Add descriptive link text** so readers can find content even if links break
1. **Consider local copies** for critical reference material in `docs/`

### CI Enforcement

The pre-commit script (`bash ./scripts/dev/pre-commit.sh`) runs link validation on all staged markdown files. **Always run pre-commit after adding or modifying markdown files** to catch broken links before they reach CI.

______________________________________________________________________

## 🔴 Documenting New Behavior

When introducing behavior that differs from previous versions or reference Lua:

### In Code Comments

```csharp
// NOTE: This behavior is NEW in NovaSharp 2.0. Previously, this method
// returned nil for invalid inputs. Now it throws ArgumentException for
// better error diagnostics.
```

### In XML Docs

```csharp
/// <remarks>
/// <para><b>New in 2.0:</b> This method now validates input ranges and throws
/// <see cref="ArgumentOutOfRangeException"/> for values outside [0, 100].</para>
/// <para><b>Migration:</b> Callers that previously relied on silent failures
/// should add try-catch blocks or validate inputs before calling.</para>
/// </remarks>
```

### In Markdown Docs

````markdown
## Breaking Changes in 2.0

### `Script.DoFile()` Validation

**Previous behavior:** Silently returned `nil` for non-existent files.

**New behavior:** Throws `FileNotFoundException` with the full path.

**Migration:** Add file existence checks or catch the exception:

```csharp
// Before (implicit nil on missing file)
DynValue result = script.DoFile(path);

// After (explicit handling required)
if (File.Exists(path))
{
    DynValue result = script.DoFile(path);
}
// Or:
try
{
    DynValue result = script.DoFile(path);
}
catch (FileNotFoundException)
{
    // Handle missing file
}
````

````

______________________________________________________________________

## Documentation Update Workflow

### Step-by-Step Process

1. **Implement the feature/fix** with inline comments for non-obvious logic
2. **Add XML documentation** to all affected public members
3. **Create/update code samples** and verify they work
4. **Update `docs/*.md`** files if user guides are affected
5. **Add CHANGELOG entry** under `[Unreleased]`
6. **Run documentation verification**:
   ```bash
   # Check for missing XML docs (compiler warnings)
   dotnet build src/NovaSharp.sln -c Release -warnaserror:CS1591
   
   # Check markdown link validity
   lychee --no-progress docs/**/*.md
````

### Documentation File Locations

| Content Type       | Location                 |
| ------------------ | ------------------------ |
| API reference      | XML docs in source files |
| User guides        | `docs/`                  |
| Changelog          | `CHANGELOG.md`           |
| Contributing guide | `docs/Contributing.md`   |
| Architecture docs  | `docs/` or `.llm/`       |

______________________________________________________________________

## Common Documentation Mistakes

### ❌ DON'T: Copy-paste incorrect examples

```csharp
/// <example>
/// <code>
/// // This was copied from somewhere and never tested
/// DynValue result = script.Call("nonexistent");  // Actually throws!
/// </code>
/// </example>
```

### ❌ DON'T: Document implementation instead of behavior

```csharp
/// <summary>
/// Iterates through the internal hash table using linear probing
/// to find the matching key entry, then returns the associated value.
/// </summary>
// ↑ User doesn't care HOW, they care WHAT
```

### ✅ DO: Document behavior and contracts

```csharp
/// <summary>
/// Gets the value associated with the specified key.
/// </summary>
/// <returns>The value if found; otherwise, <see cref="DynValue.Nil"/>.</returns>
```

### ❌ DON'T: Leave documentation stale after changes

```csharp
/// <returns>Returns true on success.</returns>  // ← Stale!
public DynValue Execute()  // ← Now returns DynValue, not bool
```

______________________________________________________________________

## Resources

- [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) — Changelog format specification
- [Semantic Versioning](https://semver.org/) — Version numbering standard
- [Microsoft XML Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/) — XML doc reference
- [docs/](../../docs/) — NovaSharp documentation folder
