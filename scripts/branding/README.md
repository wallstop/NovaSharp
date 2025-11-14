# Branding Scripts

This folder hosts guardrails that keep NovaSharp branding consistent after the MoonSharp rename.

## `ensure-novasharp-branding.sh`

```bash
bash ./scripts/branding/ensure-novasharp-branding.sh
```

- Fails the build if tracked files contain the string `MoonSharp` outside the curated allowlist (coverage baselines, benchmark history, etc.).
- Runs automatically in CI before tests and coverage; invoke manually when touching documentation or large refactors to catch regressions early.
- Respects the exclusion list baked into the script; update it when legitimately referencing MoonSharp (e.g., historical docs) so noise stays low.
