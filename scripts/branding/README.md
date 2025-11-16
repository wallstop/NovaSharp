# Branding Scripts

This folder hosts guardrails that keep NovaSharp branding consistent after the rename from the legacy brand.

## `ensure-novasharp-branding.sh`

```bash
bash ./scripts/branding/ensure-novasharp-branding.sh
```

- Fails the build if tracked files contain the legacy brand string outside the curated allowlist (coverage baselines, benchmark history, etc.).
- Runs automatically in CI before tests and coverage; invoke manually when touching documentation or large refactors to catch regressions early.
- Respects the exclusion list baked into the script; update it when legitimately referencing the legacy brand (e.g., historical docs) so noise stays low.
