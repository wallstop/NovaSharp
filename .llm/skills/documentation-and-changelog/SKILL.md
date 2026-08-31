---
name: documentation-and-changelog
description: "Update NovaSharp documentation and release-note guidance for user-visible features and fixes. Use after public API, behavior, workflow, or configuration changes and when writing XML docs or rationale comments."
metadata:
  category: workflow
  priority: recommended
  related: tunit-test-writing, lua-fixture-creation
---
# Skill: Documentation and Release-Note Management

**Related Skills**: [tunit-test-writing](../tunit-test-writing/SKILL.md) (comprehensive testing), [lua-fixture-creation](../lua-fixture-creation/SKILL.md) (test fixtures)

______________________________________________________________________

## 🔴 Critical: Documentation is NOT Optional

**Every feature and bugfix requires documentation updates.** Code without documentation is incomplete. Documentation includes:

1. **Code comments** — Non-obvious design rationale (WHY, not WHAT)
1. **XML documentation** — All public API members
1. **Markdown docs** — User guides, API references in `docs/`
1. **Code samples** — Working, tested examples
1. **GitHub Release notes** — Authoritative user-facing release history

______________________________________________________________________

## 🔴 Release-Note Requirements

NovaSharp's release-note authority is
[GitHub Releases](https://github.com/wallstop/NovaSharp/releases), which is also
the target of `PackageReleaseNotes` in
[`Directory.Build.props`](../../../Directory.Build.props). The repository does
not maintain a root `CHANGELOG.md`; do not create or reference one unless the
release process is deliberately changed. The Unity packaging scripts generate an
artifact-local changelog, but that generated file is not repository release
history.

Use Keep a Changelog-style headings in a GitHub Release description when they
make the release easier to scan:

| Category     | When to Use                                  | Example                                        |
| ------------ | -------------------------------------------- | ---------------------------------------------- |
| **Added**    | New features, API methods, or functionality  | "Add `math.type()` support for Lua 5.3+"       |
| **Changed**  | Observable or breaking behavior/API changes  | "Improve error messages for invalid arguments" |
| **Removed**  | Features or host APIs removed in the release | "Remove superseded descriptor registration"    |
| **Fixed**    | User-visible bug fixes                       | "Fix `string.format` crash with nil arguments" |
| **Security** | Vulnerability patches or security hardening  | "Fix sandbox escape via `debug` library"       |

Do not stage host-API removal through a `Deprecated` release-note section,
warning period, compatibility shim, or obsolete alias. NovaSharp is pre-adoption:
remove the superseded host surface and update repository-owned consumers in the
same change. Lua-version and supported-platform compatibility remain mandatory.

### Entry Format

Entries should be: **Concise** (one line), **User-focused** (impact over details), **Specific** (include API/feature name), **Linked** (reference issues/PRs).

### When to Prepare a Release Note

| Change Type                       | Release Note? | Category          |
| --------------------------------- | ------------- | ----------------- |
| New public API method             | ✅ YES        | Added             |
| Bug fix affecting users           | ✅ YES        | Fixed             |
| Performance improvement           | ✅ YES        | Changed           |
| Internal refactor (no behavior Δ) | ❌ NO         | —                 |
| Test additions only               | ❌ NO         | —                 |
| Documentation fixes only          | ❌ NO         | —                 |
| Breaking host-API change          | ✅ YES        | Changed / Removed |
| Security vulnerability fix        | ✅ YES        | Security          |

GitHub Releases are external state. Update a release description only when the
task authorizes release work; otherwise put a release-note-ready line in the PR
description or final handoff. Never invent a checked-in changelog as a substitute.

______________________________________________________________________

## 🔴 Documentation Checklist

### For New Features

- [ ] XML docs on all public types, methods, properties, events
- [ ] Code sample showing basic usage
- [ ] Code sample showing edge cases (if applicable)
- [ ] Update relevant `docs/*.md` files
- [ ] Prepare an `Added` release-note line for the next GitHub Release
- [ ] Note if behavior is NEW (not present in previous versions)

### For Bug Fixes

- [ ] Describe the corrected observable behavior in affected documentation and samples
- [ ] Update affected repository-owned callers and tests in the same change
- [ ] Prepare a `Fixed` release-note line for the next GitHub Release

### For Breaking Changes

- [ ] Document the new API and the removed or changed surface clearly
- [ ] Prepare a `Changed` or `Removed` release-note line and mark it as breaking
- [ ] Mark as BREAKING CHANGE explicitly
- [ ] Update all repository-owned callers, samples, tests, tooling, and docs in the
  same change
- [ ] Remove superseded host APIs directly; do not add compatibility shims,
  obsolete aliases, migration adapters, or deprecation periods

NovaSharp is pre-adoption, so breaking host-API changes do not require a
compatibility implementation or staged migration release. Release notes may
explain the replacement for clarity, but must not preserve the old surface. Lua
version behavior and supported-platform compatibility remain mandatory.

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
- **XML docs** — Describe the current contract and relevant version boundary
- **Markdown docs** — Describe the current behavior and replacement usage, without
  retaining a legacy API layer

______________________________________________________________________

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for Documentation Update Workflow, Common Documentation Mistakes, Resources.
