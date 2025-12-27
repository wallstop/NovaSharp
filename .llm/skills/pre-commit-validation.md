# Skill: Pre-Commit Validation

**When to use**: After completing ANY code changes, before considering work done.

**Related Skills**: [documentation-and-changelog](documentation-and-changelog.md) (documentation updates), [tunit-test-writing](tunit-test-writing.md) (test writing)

______________________________________________________________________

## 🔴 Critical Rule: Always Run Pre-Commit Validation

**Work is NOT complete until all pre-commit checks pass.** Before declaring any task finished, you MUST run the pre-commit validation to ensure:

1. Code is properly formatted (CSharpier, Markdown)
1. No linting errors exist
1. Branding is correct (no legacy MoonSharp references)
1. Namespaces match directory structure
1. Test infrastructure patterns are followed
1. All auto-generated files are up to date

**A diff that fails CI checks is not ready for review.**

______________________________________________________________________

## 🔴 Required Command

After making any changes, run:

```bash
bash ./scripts/dev/pre-commit.sh
```

This single command runs ALL validation checks:

| Check                     | What It Does                                  | Auto-Fixes? |
| ------------------------- | --------------------------------------------- | ----------- |
| **CSharpier**             | Formats all C# files                          | ✅ Yes      |
| **Markdown Formatting**   | Formats staged `.md` files                    | ✅ Yes      |
| **Markdown Links**        | Validates links in staged `.md` files         | ❌ No       |
| **Documentation Audit**   | Updates `docs/audits/documentation_audit.log` | ✅ Yes      |
| **Naming Audit**          | Updates `docs/audits/naming_audit.log`        | ✅ Yes      |
| **Spelling Audit**        | Updates `docs/audits/spelling_audit.log`      | ✅ Yes      |
| **Fixture Catalog**       | Regenerates `FixtureCatalogGenerated.cs`      | ✅ Yes      |
| **Branding Check**        | Ensures no MoonSharp references in new code   | ❌ No       |
| **Namespace Alignment**   | Verifies namespaces match directory structure | ❌ No       |
| **Shell Executable**      | Checks `.sh` files have executable bit        | ❌ No       |
| **Shell Python Patterns** | Validates Python invocation in shell scripts  | ❌ No       |
| **Test Lint**             | Validates test infrastructure patterns        | ❌ No       |

______________________________________________________________________

## 🔴 Workflow

### Standard Workflow

```bash
# 1. Make your changes
# 2. Build and test
./scripts/build/quick.sh
./scripts/test/quick.sh

# 3. Run pre-commit validation (REQUIRED before task is complete)
bash ./scripts/dev/pre-commit.sh

# 4. Review any errors and fix them
# 5. Re-run pre-commit until it passes
```

### When Pre-Commit Fails

If the script fails, you MUST fix the issues:

#### Branding Failures

```
[pre-commit] ERROR: MoonSharp identifier detected in staged content:
```

**Fix**: Replace `MoonSharp` with `NovaSharp` in your code.

#### Namespace Failures

```
[pre-commit] ERROR: Namespace mismatches detected.
```

**Fix**: Ensure the namespace in each file matches its directory path. Use:

```bash
python3 tools/NamespaceAudit/namespace_audit.py
```

#### Test Lint Failures

```
[pre-commit] ERROR: Test lint checks failed.
```

**Fix**: Review the specific error message. Common issues:

- Using `Path.GetTempPath()` instead of `TestPathHelper`
- Missing `UserDataIsolation` attributes
- Console capture without proper semaphore
- Using `finally` blocks that mask assertion failures

#### Shell Script Failures

```
[pre-commit] ERROR: Shell scripts missing executable bit.
```

**Fix**: Add executable permissions:

```bash
chmod +x scripts/path/to/your-script.sh
```

______________________________________________________________________

## 🔴 Individual Checks (Manual)

If you need to run specific checks individually:

### Formatting

```bash
# C# formatting
dotnet csharpier format .

# Markdown formatting (all files)
python3 scripts/ci/format_markdown.py --fix --all

# Markdown link checking
python3 scripts/ci/check_markdown_links.py --all
```

### Audits

```bash
# Namespace alignment
python3 tools/NamespaceAudit/namespace_audit.py

# Naming conventions
python3 tools/NamingAudit/naming_audit.py

# Spelling
python3 tools/SpellingAudit/spelling_audit.py

# Documentation coverage
python3 tools/DocumentationAudit/documentation_audit.py
```

### Branding

```bash
./scripts/branding/ensure-novasharp-branding.sh
```

### Test Infrastructure Lint

```bash
python3 scripts/lint/check-temp-path-usage.py
python3 scripts/lint/check-userdata-scope-usage.py
python3 scripts/lint/check-console-capture-semaphore.py
python3 scripts/lint/check-test-finally.py
```

______________________________________________________________________

## 🔴 Common Issues and Solutions

### Issue: CSharpier Changes Files Unexpectedly

This is normal — CSharpier enforces consistent formatting. The pre-commit script automatically restages formatted files.

### Issue: "dotnet tool restore" Fails

```bash
# Ensure you're in the repo root
cd /workspaces/NovaSharp
dotnet tool restore
```

### Issue: Python Tooling Not Found

```bash
# Install required Python packages
python3 -m pip install -r requirements.tooling.txt
```

### Issue: mdformat Version Mismatch

The script will attempt to auto-update. If it persists:

```bash
python3 -m pip install --upgrade mdformat==1.0.0
```

### Issue: PowerShell Not Found (Fixture Catalog)

The fixture catalog update requires PowerShell:

```bash
# On Linux/Mac
sudo apt-get install -y powershell  # Debian/Ubuntu
# OR
brew install powershell             # macOS
```

______________________________________________________________________

## 🔴 CI Enforcement

CI runs the same checks. If you skip pre-commit locally, CI will fail with:

- `scripts/ci/check-csharpier.sh` — C# formatting
- `scripts/ci/check-markdown.sh` — Markdown formatting/links
- Branding, namespace, and lint checks

**Always run pre-commit locally to catch issues before pushing.**

______________________________________________________________________

## Checklist Before Declaring Work Complete

- [ ] Code changes compile: `./scripts/build/quick.sh`
- [ ] Tests pass: `./scripts/test/quick.sh`
- [ ] Pre-commit validation passes: `bash ./scripts/dev/pre-commit.sh`
- [ ] No remaining errors or warnings from pre-commit
- [ ] Changes are ready for human review

**If any check fails, the work is NOT complete.**
