# Coverage Artifacts

Run `.\coverage.ps1` to generate coverage data into `artifacts/coverage` and an HTML dashboard under `docs/coverage/latest`. The directory `docs/coverage/latest` is ignored by Git because the HTML output is regenerated on every run; publish snapshots to long-lived locations only when needed for documentation.

The coverage script produces LCOV, Cobertura, and OpenCover formats so CI can upload whichever format is preferred, and invokes `reportgenerator` to build a browsable report that can be served from `docs/coverage`.
