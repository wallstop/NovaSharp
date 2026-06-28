______________________________________________________________________

triggers:

- "documentation"
- "changelog"
- "XML docs"
- "code comments"
- "CHANGELOG.md"
- "keep a changelog"
  category: workflow
  related:
- tunit-test-writing
- lua-fixture-creation
  priority: recommended

______________________________________________________________________

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

Entries should be: **Concise** (one line), **User-focused** (impact over details), **Specific** (include API/feature name), **Linked** (reference issues/PRs).

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

All public members require XML docs with: `<summary>`, `<param>`, `<returns>`, `<exception>`, and `<example>` where applicable.

Quality standards: **Accuracy** (match actual behavior), **Completeness** (document edge cases), **Clarity** (accessible to newcomers), **Working examples**, **Kept up-to-date**.

______________________________________________________________________

## 🔴 Code Sample Standards

Every code sample MUST: **Compile**, **Run without errors**, **Be tested**, **Be complete** (include setup), **Be minimal** (no distractions).

______________________________________________________________________

## 🔴 External Link Best Practices

- **Use canonical URLs** — `learn.microsoft.com` not `docs.microsoft.com`; full URLs not short links
- **Verify links before commit** — Run `python3 scripts/ci/check_markdown_links.py --files path/to/file.md`
- **Prefer landing pages** over deep links for frequently-changing docs
- **CI enforces link validity** via pre-commit hook

______________________________________________________________________

## 🔴 Documenting New Behavior

When introducing behavior that differs from previous versions, document the change in:

- **Code comments** — Note version where behavior changed
- **XML docs** — Use `<remarks>` with "New in X.X" and migration guidance
- **Markdown docs** — Include Previous/New behavior and migration steps

______________________________________________________________________

## Documentation Update Workflow

1. **Implement feature/fix** with inline comments for non-obvious logic
1. **Add XML documentation** to all affected public members
1. **Create/update code samples** and verify they work
1. **Update `docs/*.md`** files if user guides are affected
1. **Add CHANGELOG entry** under `[Unreleased]`
1. **Run verification**: `dotnet build -c Release -warnaserror:CS1591` and `lychee --no-progress docs/**/*.md`

### Documentation File Locations

| Content Type  | Location                 |
| ------------- | ------------------------ |
| API reference | XML docs in source files |
| User guides   | `docs/`                  |
| Changelog     | `CHANGELOG.md`           |

______________________________________________________________________

## Common Documentation Mistakes

- **Copy-paste untested examples** — All samples must be verified to work
- **Document implementation, not behavior** — Users care WHAT it does, not HOW
- **Stale documentation** — Update when behavior changes

______________________________________________________________________

## Resources

- [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) — Changelog format specification
- [Semantic Versioning](https://semver.org/) — Version numbering standard
- [Microsoft XML Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/) — XML doc reference
- [docs/](../../docs/) — NovaSharp documentation folder
