# Skill: Pre-Commit Validation

**When to use**: After completing ANY code changes, before considering work done.

**Related Skills**: [documentation-and-changelog](documentation-and-changelog.md) (documentation updates), [tunit-test-writing](tunit-test-writing.md) (test writing)

______________________________________________________________________

## 🔴 Critical Rule: Run Pre-Commit Validation Iteratively

**Run `bash ./scripts/dev/pre-commit.sh` after EVERY significant code change**, not just before declaring work complete. This catches issues early and prevents cascading failures.

> ⚠️ **Common Mistake**: Waiting until the end to run pre-commit, then discovering multiple audit failures that require extensive rework. Run it iteratively!

Before declaring any task finished, ALL pre-commit checks must pass:

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
| **Markdown Links** ⚠️     | Validates links in staged `.md` files         | ❌ No       |
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

# 3. Run pre-commit validation (run AFTER EVERY significant change, not just at the end)
bash ./scripts/dev/pre-commit.sh

# 4. Review any errors and fix them
# 5. Re-run pre-commit until it passes
# 6. Repeat steps 1-5 for each significant change
```

> 💡 **Best Practice**: Run pre-commit after each logical unit of work (e.g., after adding a new file, after refactoring a module, after adding tests). This makes debugging failures much easier than running it only at the end.

### When Pre-Commit Fails

If the script fails, you MUST fix the issues:

#### Markdown Link Failures

```
[pre-commit] ERROR: Markdown link check failed.
```

**Fix**: The link checker found broken or unreachable URLs. Common causes:

- External URLs have moved or been deleted
- Typos in internal file paths
- Old Microsoft docs URLs (use `learn.microsoft.com` instead of `docs.microsoft.com`)

To debug, run manually:

```bash
python3 scripts/ci/check_markdown_links.py --files path/to/your.md
```

See [documentation-and-changelog](documentation-and-changelog.md) for external link best practices.

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

## 🔴 Common Audit Failures and How to Prevent Them

### Files Added or Removed → Audit Log Mismatch

**Symptom**: Pre-commit fails with diff in `docs/audits/spelling_audit.log`, `docs/audits/naming_audit.log`, or `docs/audits/documentation_audit.log`.

**Cause**: When files are added, removed, or renamed, the audit logs become stale because they track all scanned files.

**Prevention**:

1. **Run pre-commit immediately after adding/removing files** — Don't wait until the end

1. Pre-commit auto-regenerates these logs, but you must commit the updated logs

1. If manually regenerating:

   ```bash
   python3 tools/SpellingAudit/spelling_audit.py --write-log docs/audits/spelling_audit.log
   python3 tools/NamingAudit/naming_audit.py --write-log docs/audits/naming_audit.log
   python3 tools/DocumentationAudit/documentation_audit.py --write-log docs/audits/documentation_audit.log
   ```

### Fixture Catalog Out of Sync

**Symptom**: Pre-commit shows diff in `FixtureCatalogGenerated.cs`.

**Cause**: Lua fixture files (`.lua`) were added, removed, or had their metadata changed.

**Prevention**: Run pre-commit after adding/modifying any `.lua` test fixtures. The catalog is regenerated automatically.

### Iterative Development Anti-Pattern

**Symptom**: Multiple cascading failures when pre-commit is run only at the end of a large task.

**Cause**: Making many changes without running pre-commit leads to compounding issues:

- Spelling audit finds the old file list
- Naming audit finds stale namespaces
- Branding check finds issues in multiple files
- Each fix requires re-running, which may reveal more issues

**Prevention**:

> 🔴 **Run `bash ./scripts/dev/pre-commit.sh` after EVERY significant change:**
>
> - After adding a new file
> - After removing or renaming a file
> - After refactoring that moves code between files
> - After adding new public APIs
> - Before switching to a different area of the codebase

This "fail fast" approach catches issues when there's only one thing to fix, not twenty.

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
- [ ] **If any files were added/removed**: Audit logs are regenerated (pre-commit does this automatically)
- [ ] Pre-commit validation passes: `bash ./scripts/dev/pre-commit.sh`
- [ ] No remaining errors or warnings from pre-commit
- [ ] **If CI/CD workflows were modified**: Run affected CI scripts locally (see below)
- [ ] **Final verification**: Run pre-commit one more time to confirm all auto-generated files are committed
- [ ] Changes are ready for human review

**If any check fails, the work is NOT complete.**

______________________________________________________________________

## 🔴 CI/CD Workflow Validation

If you modify any files in `.github/workflows/` or build/test scripts, **you MUST validate them locally before pushing**. See [ci-cd-validation](ci-cd-validation.md) for the full guide.

### Quick CI Validation

```bash
# Run the same scripts CI uses
./scripts/build/build.sh                        # Full build (what CI runs)
./scripts/branding/ensure-novasharp-branding.sh # Branding check
./scripts/ci/check-csharpier.sh                 # CSharpier gate
./scripts/ci/check-markdown.sh                  # Markdown formatting + links
```

### Verify Test Artifact Generation

If modifying test execution or artifact paths:

```bash
# Run tests and verify artifacts are created
./scripts/test/quick.sh
ls -la artifacts/test-results/

# Check that TRX files exist (required for CI artifact upload)
find artifacts -name "*.trx"
```

### Common CI Issues to Check For

| Issue                    | Symptom                        | Prevention                                          |
| ------------------------ | ------------------------------ | --------------------------------------------------- |
| Missing TRX package      | No test results artifact       | Ensure `Microsoft.Testing.Extensions.TrxReport` ref |
| Platform options ignored | Options have no effect         | Put options after `--` separator                    |
| Path mismatch            | Artifact upload finds no files | Verify paths between test run and upload step       |
| Shell script not +x      | Permission denied              | Run `chmod +x` on new scripts                       |
