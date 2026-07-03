# Session 130: Phase A0 Unity IL2CPP Spot Check

Date: 2026-07-03

## Summary

- Added tracked Unity Package Manager sample templates under `src/unity/com.wallstop-studios.novasharp/Samples~`.
- Added `IL2CPPSpotCheck`, a minimal Unity scene with one `IL2CPPSpotCheckRunner` component.
- The runner compiles a small Lua chunk once, warms the call path, times pure Lua calls, table mutation, and a simple Lua-to-CLR callback with `System.Diagnostics.Stopwatch`, then writes one player-log line beginning with `NOVASHARP_IL2CPP_SPOTCHECK PASS`.
- Reworked both Unity package builders to copy tracked sample templates instead of embedding sample C# in Bash and PowerShell.
- Added early package-builder overlap guards so generated output cannot overlap the tracked package or sample templates before any output directories are created.
- Added a narrow `.gitignore` exception so the literal UPM `Samples~` source directory and its `.meta` files can be tracked.
- Updated the namespace audit to skip Unity UPM sample templates because package paths such as `Samples~` cannot map to valid C# namespace tokens.
- Updated packaging and Unity integration docs to describe the spot-check sample.

## Validation

- `bash -n scripts/packaging/build-unity-package.sh` exited 0.
- `pwsh -NoProfile -Command '[scriptblock]::Create((Get-Content -Raw scripts/packaging/build-unity-package.ps1)) | Out-Null'` exited 0.
- `./scripts/packaging/build-unity-package.sh --version 3.0.0-dev --output src/unity` and `./scripts/packaging/build-unity-package.sh --version 3.0.0-dev --output src/unity/com.wallstop-studios.novasharp/Samples~` both failed before publishing, as expected.
- `pwsh -NoProfile -File scripts/packaging/build-unity-package.ps1 -Version 3.0.0-dev -OutputPath src/unity` and `pwsh -NoProfile -File scripts/packaging/build-unity-package.ps1 -Version 3.0.0-dev -OutputPath src/unity/com.wallstop-studios.novasharp/Samples~` both failed before publishing, as expected.
- `find src/unity/com.wallstop-studios.novasharp -maxdepth 4 -type d | sort` confirmed the negative guard checks did not create nested package directories in the tracked template tree.
- `./scripts/packaging/build-unity-package.sh --version 3.0.0-dev --output artifacts/unity-spotcheck-validation-current` exited 0 after the early-overlap guard hardening.
- `pwsh -NoProfile -File scripts/packaging/build-unity-package.ps1 -Version 3.0.0-dev -OutputPath artifacts/unity-spotcheck-validation-ps-current` exited 0 after the early-overlap guard hardening.
- `python3 -m json.tool artifacts/unity-spotcheck-validation-current/com.wallstop-studios.novasharp/package.json` and `python3 -m json.tool artifacts/unity-spotcheck-validation-ps-current/com.wallstop-studios.novasharp/package.json` exited 0.
- `find artifacts/unity-spotcheck-validation-current/com.wallstop-studios.novasharp/Samples~ artifacts/unity-spotcheck-validation-ps-current/com.wallstop-studios.novasharp/Samples~ -type f | sort` confirmed both generated packages include `NovaSharpBasicUsage.cs`, its matching `.meta`, and the IL2CPP spot-check scene files.
- `python3 tools/NamespaceAudit/namespace_audit.py` exited 0 after the Unity sample-template skip was added.
- `bash ./scripts/dev/pre-commit.sh` exited 0 after the sample XML docs, serialized field naming, and namespace audit fixes.
- `git diff --check` exited 0 after the guard hardening.
- An adversarial sub-agent review found a Unity class/file-name mismatch in the Basic Usage sample and a same-path output guard risk in the package builders; both were fixed.
- A second adversarial review found that the output guard ran too late and missed generated outputs nested under the tracked sample tree; that class of issue was fixed with early path-overlap guards and the negative checks above.
- A third adversarial review reported no findings after the guard hardening and sample checks.

## Residual Risk

- Unity is not installed in this local environment, so an actual IL2CPP player build and player-log pass line were not observed locally.
- The spot-check is intentionally a stopwatch smoke test. It should catch gross Unity player path failures, but it is not a BenchmarkDotNet replacement and should not be treated as a precise performance gate.
