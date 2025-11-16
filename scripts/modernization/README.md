# Modernization Scripts

Utilities that support the modernization roadmap live here.

## `generate-moonsharp-audit.ps1`

```powershell
pwsh ./scripts/modernization/generate-moonsharp-audit.ps1
```

- Scans the repository for references to the legacy brand and emits a classified audit (kept vs. removed vs. follow-up) to help track rename progress.
- Run this script whenever you add/remove major components so `docs/modernization/moonsharp-issue-audit.md` and related trackers stay up to date.
- Requires PowerShell (`pwsh`) and the same tooling prerequisites as the rest of the repo; invoke from the repository root.
