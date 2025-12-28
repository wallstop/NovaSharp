# Skill: CI/CD Validation

**When to use**: Modifying GitHub Actions workflows, build scripts, test infrastructure, or any CI/CD configuration.

**Related Skills**: [pre-commit-validation](pre-commit-validation.md) (local validation), [tunit-test-writing](tunit-test-writing.md) (test configuration), [test-failure-investigation](test-failure-investigation.md) (investigating CI failures)

______________________________________________________________________

## 🔴 Critical Rule: Verify CI Changes Locally Before Pushing

**Every CI/CD change MUST be validated locally before pushing.** CI failures from untested workflow changes waste time and block PRs. Never push CI changes without first running the equivalent scripts locally.

______________________________________________________________________

## Microsoft.Testing.Platform / TUnit Requirements

NovaSharp uses **TUnit** as its test framework, which runs on **Microsoft.Testing.Platform** (MSTest-style test execution). This has specific requirements:

### Package Requirements

The test project MUST reference these packages:

```xml
<!-- Core TUnit packages -->
<PackageReference Include="TUnit" Version="..." />
<PackageReference Include="TUnit.Assertions" Version="..." />
<PackageReference Include="TUnit.Engine" Version="..." />

<!-- Required for TRX report generation -->
<PackageReference Include="Microsoft.Testing.Extensions.TrxReport" Version="..." />
```

> ⚠️ **Common Mistake**: Forgetting `Microsoft.Testing.Extensions.TrxReport` causes CI to fail when trying to generate test result artifacts.

### Platform Options Separator

When passing options to `dotnet test` for Microsoft.Testing.Platform, options intended for the test platform itself **MUST** come after the `--` separator:

```bash
# ❌ WRONG: Platform options mixed with dotnet test options
dotnet test --results-directory ./results --report-trx

# ✅ CORRECT: Platform options after -- separator
dotnet test -- --results-directory ./results --report-trx

# ✅ CORRECT: Using dotnet run (recommended for TUnit)
dotnet run --project Test.csproj --no-build -- --results-directory ./results --report-trx
```

### TRX Report Generation

To generate TRX files for CI artifact upload:

```bash
# Use the test project's built-in support
dotnet run --project Test.csproj --no-build -- \
    --results-directory ./artifacts/test-results \
    --report-trx \
    --report-trx-filename test-results.trx
```

______________________________________________________________________

## Validating CI Changes Locally

### 1. Run the Same Scripts CI Uses

Before modifying a workflow, identify what scripts it runs and execute them locally:

```bash
# Check which scripts a workflow uses
cat .github/workflows/tests.yml | grep -E "run:|shell:"

# Run the build script that CI uses
./scripts/build/build.sh

# Run specific CI check scripts
./scripts/ci/check-csharpier.sh
./scripts/ci/check-markdown.sh
./scripts/branding/ensure-novasharp-branding.sh
```

### 2. Verify Test Artifact Generation

If modifying test execution or artifact paths:

```bash
# Build and run tests with artifact generation
./scripts/build/quick.sh
./scripts/test/quick.sh

# Verify artifacts were created
ls -la artifacts/test-results/

# Check TRX file was generated (if CI expects it)
find artifacts -name "*.trx"
```

### 3. Simulate CI Environment

For complex workflow changes, simulate the CI environment:

```bash
# Run tests with the same configuration CI uses
dotnet test src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit \
    --configuration Release \
    --no-restore \
    -- --results-directory ./artifacts/test-results --report-trx
```

______________________________________________________________________

## GitHub Actions Workflow Validation

### YAML Syntax Validation

```bash
# Install actionlint (GitHub Actions linter)
# On Ubuntu/Debian devcontainer:
gh release download --repo rhysd/actionlint --pattern '*_linux_amd64.tar.gz'
tar xzf actionlint_*_linux_amd64.tar.gz
./actionlint .github/workflows/*.yml
```

### Common Workflow Issues

| Issue                           | Symptom                                                                     | Fix                                                       |
| ------------------------------- | --------------------------------------------------------------------------- | --------------------------------------------------------- |
| **Missing package**             | `error MSB4019: The imported project was not found`                         | Add missing `<PackageReference>`                          |
| **TRX not generated**           | `Error: No files were found with the provided path: artifacts/test-results` | Add `Microsoft.Testing.Extensions.TrxReport` package      |
| **Platform options ignored**    | Options like `--results-directory` have no effect                           | Move options after `--` separator                         |
| **Shell script not executable** | `Permission denied`                                                         | Run `chmod +x script.sh`                                  |
| **Path mismatch**               | Artifact upload finds no files                                              | Verify paths match between test execution and upload step |

### Artifact Path Verification

When modifying artifact paths, verify consistency:

```yaml
# In tests.yml - these paths MUST match:
- name: Run tests
  run: dotnet run --project Test.csproj -- --results-directory ./artifacts/test-results

- name: Upload test results  
  uses: actions/upload-artifact@v5
  with:
    path: artifacts/test-results  # Must match --results-directory above
    if-no-files-found: error      # Fail fast if paths don't match
```

______________________________________________________________________

## 🔴 CI/CD Change Checklist

Before pushing any CI/CD changes:

- [ ] **Identified affected scripts**: List all scripts the workflow calls
- [ ] **Ran scripts locally**: Executed each script in the workflow locally
- [ ] **Verified artifact generation**: Confirmed expected files are created
- [ ] **Checked path consistency**: Artifact paths match between generation and upload
- [ ] **Tested on clean state**: Ran from clean build to catch missing dependencies
- [ ] **Validated YAML syntax**: Used `actionlint` or similar for workflow files
- [ ] **Ran pre-commit**: `bash ./scripts/dev/pre-commit.sh` passes

______________________________________________________________________

## Common CI Pitfalls and How to Avoid Them

### 1. Missing Test Result Artifacts

**Problem**: CI uploads artifacts but finds no files.

**Cause**:

- Missing `Microsoft.Testing.Extensions.TrxReport` package
- Wrong `--results-directory` path
- Options placed before `--` separator

**Prevention**:

```bash
# Always verify locally that artifacts are generated
./scripts/test/quick.sh
ls -la artifacts/test-results/
```

### 2. Tests Pass Locally But Fail in CI

**Problem**: Tests work on developer machine but fail in GitHub Actions.

**Causes**:

- Missing isolation attributes (race conditions)
- Environment-dependent code (file paths, temp directories)
- Timing-dependent tests

**Prevention**:

- Use `[UserDataIsolation]`, `[ScriptGlobalOptionsIsolation]` attributes
- Use `TestPathHelper` instead of `Path.GetTempPath()`
- Run tests multiple times locally with `./scripts/test/quick.sh`

### 3. Workflow YAML Syntax Errors

**Problem**: Workflow fails to parse.

**Prevention**:

```bash
# Validate before pushing
./actionlint .github/workflows/*.yml
```

### 4. Package Restore Failures

**Problem**: `--locked-mode` restore fails.

**Cause**: `packages.lock.json` is out of sync with package references.

**Prevention**:

```bash
# Regenerate lock file after package changes
dotnet restore src/NovaSharp.sln --force-evaluate
```

### 5. Shell Script Permission Issues

**Problem**: `Permission denied` when running `.sh` scripts in CI.

**Prevention**:

- Pre-commit checks for executable bit
- Run `chmod +x` on new scripts
- Check with `./scripts/ci/check-shell-executable.sh`

______________________________________________________________________

## Quick Reference: NovaSharp CI Structure

```text
.github/workflows/
├── tests.yml           # Main test workflow (lint → tests → coverage)
├── csharpier.yml       # CSharpier formatting check
├── benchmarks.yml      # Performance benchmarks
└── nuget-publish.yml   # Package publishing

scripts/
├── build/
│   ├── build.sh        # Full build script (used by CI)
│   ├── build.ps1       # Windows variant
│   └── quick.sh        # Developer quick build
├── test/
│   └── quick.sh        # Developer quick test
├── ci/
│   ├── check-csharpier.sh       # CSharpier formatting gate
│   ├── check-markdown.sh        # Markdown formatting + links
│   ├── check-fixture-catalog.ps1 # Fixture catalog sync check
│   └── apply-formatters.sh      # Auto-fix formatting issues
├── coverage/
│   └── coverage.ps1    # Coverage report generation
└── dev/
    └── pre-commit.sh   # Local pre-commit validation (run this!)
```

______________________________________________________________________

## Running CI Checks Locally

The most important script to run before pushing:

```bash
# This simulates most of what CI does
bash ./scripts/dev/pre-commit.sh
```

For a more comprehensive CI simulation:

```bash
# Full CI-equivalent validation
./scripts/build/build.sh
./scripts/branding/ensure-novasharp-branding.sh
python3 tools/NamespaceAudit/namespace_audit.py
./scripts/ci/check-csharpier.sh
./scripts/ci/check-markdown.sh
```

______________________________________________________________________
