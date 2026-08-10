---
name: ci-workflow
description: "Run and troubleshoot NovaSharp pre-commit and CI closure gates. Use before completing code changes or when modifying GitHub Actions, formatting, validation, build, test, coverage, or benchmark workflows."
metadata:
  category: workflow
  priority: core
  related: tunit-test-writing, test-failure-investigation
---
# Skill: CI/CD Workflow

**Related Skills**: [tunit-test-writing](../tunit-test-writing/SKILL.md), [test-failure-investigation](../test-failure-investigation/SKILL.md)

______________________________________________________________________

## Pre-Commit Validation (REQUIRED)

**Run after EVERY significant code change**, not just before finishing:

```bash
bash ./scripts/dev/pre-commit.sh
```

This runs ALL validation checks:

| Check            | What It Does                           | Auto-Fixes? |
| ---------------- | -------------------------------------- | ----------- |
| CSharpier        | Formats all C# files                   | Yes         |
| Markdown Format  | Formats staged `.md` files             | Yes         |
| Markdown Links   | Validates links in staged `.md` files  | No          |
| Documentation    | Updates documentation_audit.log        | Yes         |
| Naming Audit     | Updates naming_audit.log               | Yes         |
| Spelling Audit   | Updates spelling_audit.log             | Yes         |
| Fixture Catalog  | Regenerates FixtureCatalogGenerated    | Yes         |
| Branding Check   | No legacy-brand references in new code | No          |
| Namespace Align  | Namespaces match directory structure   | No          |
| Shell Executable | `.sh` files have executable bit        | No          |
| Tooling Setup    | Devcontainer/hooks/CI match tool pins  | No          |
| YAML Lint        | Staged YAML is syntactically valid     | No          |
| Actionlint       | GitHub Actions workflows are valid     | No          |
| Test Lint        | Test infrastructure patterns           | No          |

______________________________________________________________________

## Standard Workflow

```bash
# 1. Make your changes

# 2. Build and test
./scripts/build/quick.sh
./scripts/test/quick.sh

# 3. Run pre-commit validation
bash ./scripts/dev/pre-commit.sh

# 4. Fix any errors, re-run pre-commit
# 5. Repeat for each significant change
```

______________________________________________________________________

## Common Pre-Commit Failures

### Branding Failures

```text
[pre-commit] ERROR: Legacy brand identifier detected
```

**Fix**: Replace the old Moon Sharp brand string with `NovaSharp` in your code.

### Namespace Failures

```text
[pre-commit] ERROR: Namespace mismatches detected.
```

**Fix**: Ensure namespaces match directory paths:

```bash
python3 tools/NamespaceAudit/namespace_audit.py
```

### Test Lint Failures

Common issues:

- Using `Path.GetTempPath()` instead of `TestPathHelper`
- Missing `UserDataIsolation` attributes
- Console capture without proper semaphore
- Using `finally` blocks that mask assertion failures

### Shell Script Failures

```bash
chmod +x scripts/path/to/your-script.sh
```

______________________________________________________________________

## Additional guidance

Read [the detailed reference](references/REFERENCE.md) for CI/CD Validation, TUnit / Microsoft.Testing.Platform, CI Structure, Common CI Pitfalls, and later sections.
