## Summary

- Provide a short summary of the change.

## Testing

- List each command/check run, platform, and result. Mark any required check that was not run as `not run` with residual risk.
- Review the sticky benchmark comparison delta and Lua comparison comments when CI updates them; call out any production fix, test fix, or intentionally accepted residual risk here.

## Analyzer Coverage

- [ ] Ran `dotnet build src/NovaSharp.sln -c Release -nologo`
- Additional analyzer/build/test commands (list each, remove this line if none beyond the solution build):
  - _example: `dotnet build src/debuggers/WallstopStudios.NovaSharp.RemoteDebugger/WallstopStudios.NovaSharp.RemoteDebugger.csproj -c Release -nologo`_

## Checklist

- [ ] Updated relevant docs (`docs/README.md`, feature-specific Markdown) when adding or changing functionality.
- [ ] Updated `scripts/README.md` and the subfolder README when adding/modifying helper scripts.
- [ ] Listed exact local verification commands and results, or marked unrun checks as `not run`.
- [ ] PR CI is green, or any failing/pending check is named with current diagnosis.
- [ ] Benchmark comparison/Lua comparison PR comments reviewed when those workflows ran.
