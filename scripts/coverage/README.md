# Coverage Scripts

This folder hosts the cross-platform wrappers that drive Coverlet + ReportGenerator for NovaSharp.

## PowerShell

```powershell
pwsh ./scripts/coverage/coverage.ps1 [-SkipBuild] [-Configuration Release] [-MinimumInterpreterCoverage 70]
```

- Restores local dotnet tools, builds the repo (unless `-SkipBuild`), runs the interpreter test project under Coverlet, and emits LCOV/Cobertura/OpenCover reports.
- Generates HTML/Markdown/JSON summaries under `docs/coverage/latest` and copies the Markdown/JSON snapshots into `artifacts/coverage`.
- Fails if `NovaSharp.Interpreter` line coverage drops below the `-MinimumInterpreterCoverage` threshold (default 70%).

## Bash (macOS/Linux fallback)

```bash
bash ./scripts/coverage/coverage.sh [--skip-build] [--configuration Release] [--minimum-interpreter-coverage 70]
```

- Mirrors the PowerShell script behaviour for environments without PowerShell (`pwsh`/`powershell`).
- Requires `python3` (or `python`) for parsing the JSON summary during the threshold check.

## Tips

- Run both scripts from the repo root so relative paths resolve correctly.
- Set `NOVASHARP_COVERAGE_SUMMARY=1` to force full summary output even outside CI, or `0` to suppress it locally.
- CI jobs call the PowerShell script by default; GitHub runners without PowerShell can switch to the Bash variant.
