# CI/CD Workflow Reference

## CI/CD Validation

When modifying workflows or scripts, verify locally first:

### Run the Same Scripts CI Uses

```bash
./scripts/build/build.sh                        # Full build
./scripts/branding/ensure-novasharp-branding.sh # Branding check
./scripts/ci/check-csharpier.sh                 # CSharpier gate
./scripts/ci/check-markdown.sh                  # Markdown check
./scripts/ci/check-tooling-consistency.sh       # Devcontainer/hook/tooling guard
yamllint -c .yamllint.yml .github .github/dependabot.yml # YAML syntax/style
actionlint                                      # GitHub Actions workflows
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

- [ ] Record the exact build command and observed result: `./scripts/build/quick.sh`
- [ ] Record the exact test command and observed result: `./scripts/test/quick.sh`
- [ ] Record the exact formatter/pre-commit command and observed result: `bash ./scripts/dev/pre-commit.sh`
- [ ] For behavior changes, record the exact Lua comparison command and observed result.
- [ ] For PR work, poll GitHub Actions until the PR run is green or document the newly diagnosed failing check.
- [ ] Mark any unrun check as `not run` and residual risk.

Only say `green`, `verified`, `passes`, or `complete` when the exact check was observed passing. If any required check fails or was not run, the work is not green-lighted.
