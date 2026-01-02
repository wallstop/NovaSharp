______________________________________________________________________

triggers:

- "CI"
- "pre-commit"
- "workflow"
- "GitHub Actions"
- "formatting"
- "validation"
  category: workflow
  related:
- tunit-test-writing
- test-failure-investigation
  priority: core

______________________________________________________________________

# Skill: CI/CD Workflow

**When to use**: Before completing ANY code changes, and when modifying CI/CD configuration.

**Related Skills**: [tunit-test-writing](tunit-test-writing.md), [test-failure-investigation](test-failure-investigation.md)

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

## CI/CD Validation

When modifying workflows or scripts, verify locally first:

### Run the Same Scripts CI Uses

```bash
./scripts/build/build.sh                        # Full build
./scripts/branding/ensure-novasharp-branding.sh # Branding check
./scripts/ci/check-csharpier.sh                 # CSharpier gate
./scripts/ci/check-markdown.sh                  # Markdown check
```

### Verify Test Artifacts

```bash
./scripts/test/quick.sh
ls -la artifacts/test-results/
find artifacts -name "*.trx"
```

______________________________________________________________________

## TUnit / Microsoft.Testing.Platform

NovaSharp uses TUnit on Microsoft.Testing.Platform.

### Package Requirements

```xml
<PackageReference Include="TUnit" Version="..." />
<PackageReference Include="TUnit.Assertions" Version="..." />
<PackageReference Include="TUnit.Engine" Version="..." />
<PackageReference Include="Microsoft.Testing.Extensions.TrxReport" Version="..." />
```

### Platform Options Separator

Options for the test platform MUST come after `--`:

```bash
# WRONG
dotnet test --results-directory ./results --report-trx

# CORRECT
dotnet test -- --results-directory ./results --report-trx
```

______________________________________________________________________

## CI Structure

```text
.github/workflows/
├── tests.yml           # Main test workflow
├── csharpier.yml       # CSharpier check
├── benchmarks.yml      # Performance benchmarks
└── nuget-publish.yml   # Package publishing

scripts/
├── build/
│   ├── build.sh        # Full build (CI uses this)
│   └── quick.sh        # Developer quick build
├── test/
│   └── quick.sh        # Developer quick test
├── ci/
│   ├── check-csharpier.sh
│   └── check-markdown.sh
└── dev/
    └── pre-commit.sh   # Run this before pushing!
```

______________________________________________________________________

## Common CI Pitfalls

| Issue                    | Symptom                  | Fix                                     |
| ------------------------ | ------------------------ | --------------------------------------- |
| Missing TRX package      | No test results artifact | Add TrxReport package                   |
| Platform options ignored | Options have no effect   | Put options after `--` separator        |
| Path mismatch            | Artifact upload fails    | Verify paths match test and upload step |
| Shell script not +x      | Permission denied        | `chmod +x` on new scripts               |
| Package lock stale       | --locked-mode fails      | `dotnet restore --force-evaluate`       |

______________________________________________________________________

## Checklist Before Declaring Work Complete

- [ ] Code compiles: `./scripts/build/quick.sh`
- [ ] Tests pass: `./scripts/test/quick.sh`
- [ ] Pre-commit passes: `bash ./scripts/dev/pre-commit.sh`
- [ ] No remaining errors or warnings
- [ ] Changes are ready for human review

**If any check fails, the work is NOT complete.**
