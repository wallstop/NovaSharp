# Performance History Archive

Use this directory to capture historical BenchmarkDotNet snapshots whenever a run materially changes the NovaSharp performance story.

- Name each file `YYYY-MM-DD.md` and include both the NovaSharp and comparison tables for that run.
- Link back to the source PR or issue so future readers understand why the snapshot was taken.
- Keep data immutable after publishing—subsequent updates should add a new file rather than rewriting an existing one.
- When a snapshot informs release notes, mirror the highlight summary in `docs/Performance.md` and cross-link the archive file for detail.

## Current Self Baseline

CI looks for checked-in BenchmarkDotNet JSON artifacts under `current-baseline/` when rendering NovaSharp-vs-NovaSharp self deltas. That directory is intentionally absent until the project chooses a canonical baseline capture. External runtime deltas do not use this directory; they are generated from MoonSharp, NLua, and NovaSharp artifacts produced on the same runner in the same benchmark workflow job.
