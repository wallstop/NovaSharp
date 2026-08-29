# Session 178 — A5 and repository integrity closure

Date: 2026-08-29

## Objective

Carry the existing Basic-module A5 work, PLAN hygiene, and devcontainer tooling
forward to one reviewable branch; close the discovered Lua corpus integrity gaps;
and validate the aggregate before opening one PR.

## Starting state

- Branch `a5-basic-callback-views` was one commit ahead of `main` at `1e2561b9`
  and already existed on the remote, but had no upstream configuration and no PR.
- The committed A5 slice migrated Basic-module callbacks to
  `CallbackArgumentsView` and added allocation/view regressions.
- The worktree also held two uncommitted carryovers:
  - session 177's lean PLAN conversion and anti-bloat guard;
  - devcontainer npm-tool refresh, artifact-retention, and shared GitHub MCP work.
- Repository-local `package.json`, `package-lock.json`, and `.nanocoder/tasks.json`
  were unrelated local artifacts and remained outside the project change.
- `stash@{0}` contained an obsolete pre-main Lua-random rollback and was neither
  applied nor removed.

## GitHub audit

The complete paginated search returned 20 open issues with
`incomplete_results=false`:

`#84`, `#85`, `#87`, `#88`, `#92`, `#93`, `#94`, `#95`, `#98`, `#99`, `#100`,
`#101`, `#102`, `#103`, `#104`, `#105`, `#106`, `#108`, `#113`, and `#114`.

Gameplay-impact ordering remains A5 issue `#108`, followed by the measured table
and allocation work in `#93` and `#92`. Issue `#95` is the umbrella rather than
an independent implementation slice. Issues `#103`, `#104`, `#88`, and duplicate
hash-table issues `#87`/`#105` remain speculative until measurements justify
them. Correctness and CI-assurance issues `#98`, `#99`, `#100`, `#101`, and `#114`
take precedence over speculative optimization.

`main` at `ca18d658` had 59 successful checks and two expected conditional skips:
three successful push workflows (Tests, CSharpier, and Benchmarks) plus a
successful dynamic Pages deployment. There were no failing or pending checks.

Exactly one open PR existed and none were drafts. Dependabot PR `#115` upgrades
Coverlet 6 to 10, but it was unstable rather than green: coverage was cancelled,
the branch was not rebaseable, and an unresolved high-severity review identified
the known .NET 8/System.Runtime 9 incompatibility. Issue `#114` already records
the RCA and safe retry requirements, so the dependency was not incorporated.

## Lua corpus red observations

The first source extraction found 1,975 snippets but only 1,974 generated fixture
headers and 1,970 manifest entries. The new source/manifest/fixture-set probe
failed on duplicate paths, missing paths, and one newly added Basic fixture. This
confirmed both reported failure modes:

- distinct snippets sharing a short class and method name could overwrite the
  same output path (`#101`);
- hundreds of retained fixtures no longer had a current extracted source owner,
  but the extractor neither surfaced nor safely triaged them (`#100`).

A naive first regeneration was intentionally rejected. It changed 1,880 fixtures,
including 1,859 source-line-only edits, reordered most of the manifest, and would
have overwritten curated standalone fixture bodies. Those generated changes were
rolled back while preserving the pre-existing callback fixture edit. This red
experiment established that regeneration had to preserve bodies, provenance, and
ordering rather than merely produce unique filenames.

The first idempotence run then exposed a second defect: leading Lua comments were
parsed as fixture header lines, so comment-led programs acquired a new numeric
suffix on every run. The integration guard failed with the exact drifting paths
until body comparison reconstructed the absorbed comment prefix.

Finally, the recovered paths exposed existing heuristic defects tracked by
`#99`: free host variables such as `ud`, `handle`, and `assertLocal` were sometimes
marked reference-comparable, while a string literal containing `userdata` was
marked NovaSharp-only. The first Lua 5.1–5.5 comparison consequently found one new
both-error entry for the injected `assertLocal` callback. Curating the recovered
headers removed that false comparison classification.

## Implementation

### A5 Basic callbacks

- Completed the Basic-module `CallbackArgumentsView` migration already carried
  by the branch.
- Preserved multi-return forwarding without a synthetic `Void` sentinel and
  covered registered callbacks, wrapped callbacks, and view-only access.
- Removed the completed Basic slice from PLAN; Debug is now the first remaining
  CoreLib registration in A5.

### Lean execution queue

- Replaced the 1,507-line historical PLAN with the 73-line current/future queue
  described in session 177.
- Added and indexed the plan-maintenance skill.
- Added a focused 120-line hygiene ceiling and history-shape regressions, wired
  into Markdown CI and pre-commit.
- Routed detailed history, architecture, security, and research context to
  progress records, domain documents, and GitHub issues.

### Devcontainer lifecycle

- Installed current Node.js LTS and the latest Nanocoder, OpenCode, and Codex npm
  CLIs as the non-root container user, with UID-remap-safe shared permissions.
- Added bounded create/start refresh behavior with a tested offline fallback.
- Replaced the unused build-cache volume with explicit generated-artifact cleanup
  and seven-day start-time retention.
- Added secret-free GitHub hosted-MCP configurations for VS Code, Codex,
  Claude/Copilot/Nanocoder, and OpenCode.
- Added a consistency checker and network-free lifecycle harness. Docker-backed
  no-cache verification remains conditional when Docker is unavailable.

### Corpus integrity

- Made C# discovery deterministic.
- Reconciled snippets by logical source slot and Lua body:
  - exact duplicates at the same slot collapse to one fixture;
  - distinct colliding bodies receive stable numeric paths;
  - matching existing bodies retain their path and curated metadata;
  - nonmatching existing bodies reserve their path and are never overwritten.
- Reconstructed leading comment prefixes during body comparison so generation is
  idempotent for comment-led programs.
- Preserved `@source` line numbers when the source file is unchanged and kept
  existing manifest order, appending only new paths.
- Added default orphan count/preview reporting and `--report-orphans` for the full
  list. The extractor never deletes reported fixtures.
- Added a source/manifest/fixture completeness guard plus 32 focused extractor
  regressions.
- Wired the focused extractor suite into the Lua comparison CI lane so collision,
  idempotence, and orphan regressions cannot bypass pull-request validation.
- Regenerated a 1,957-entry unique manifest and recovered 15 distinct fixture
  programs that had no unique path. Twenty-four exact duplicate programs at the
  same logical slot are represented once.
- Curated recovered metadata against the C# setup and reference-Lua behavior.

### Independent-review correction

A zero-knowledge aggregate review rejected the first candidate because the
existing NovaSharp `warn` implementation did not match Lua 5.4/5.5: it emitted
by default, joined arguments with tabs, treated controls as ordinary payloads,
and routed fallback output through the debug printer. The same review found that
one common Basic fixture was incorrectly restricted to Lua 5.1 and that the 32
extractor regressions were not yet CI-wired.

The corrected implementation now keeps warning state per script, starts disabled,
recognizes exact single-argument `@on`/`@off` controls, ignores unknown control
messages, validates every argument while disabled, concatenates string/number
arguments without separators, and emits Lua's `Lua warning: ` prefix through
configured standard error or process standard error. The NovaSharp `_WARN` host
hook receives the raw concatenated payload. Focused tests cover Lua 5.4 and 5.5,
including default state, controls, validation, handler interception, configured
stderr, and console stderr. Reference comparison fixtures and the both-error
ratchet cover the same behavior.

A second adversarial review requested direct proof that enabling warnings cannot
leak between scripts and that writing a warning does not close a host-owned
`ScriptOptions.Stderr` stream. Those regressions were added and passed after a
forced test-project rebuild. It also challenged the shared MCP files as local
configuration. Independent adjudication retained them because they were recorded
in-progress carryover required by this goal, configure the CLIs installed by the
same devcontainer change, use only opt-in environment references or VS Code OAuth,
and contain no credential. The separate local `.nanocoder` and npm artifacts
remain excluded.

After the first pushed head passed all Tests, Benchmarks, and CSharpier workflows,
Cursor Bugbot found two devcontainer lifecycle defects. The npm refresh now accepts
a nonzero `npm list` diagnostic exit only when the emitted dependency tree is valid
JSON, and both initial and post-install fallback reads share that validation. The
retention cleanup now uses an exact Python-created cutoff reference with portable
`find -newer` comparisons instead of platform-dependent `find -mtime` rounding or
GNU-only `date --date`/`find -newermt`; the tooling checker prevents those
host-portability regressions. Focused lifecycle tests reproduce the npm
diagnostic-exit case and the exact 7-day cleanup boundary for files, symlinks,
`bin`, and `obj`. Injected cutoff-clock and directory-traversal failures verify
that cleanup exits nonzero, preserves build output, and removes temporary files.

## Validation receipt before PR

- Extractor regeneration is idempotent: a second full run preserved both the
  manifest SHA-256 and the untracked/generated-file-list SHA-256.
- Final extractor report: 1,957 snippets, 491 NovaSharp-only, 1,466 comparable,
  and 359 existing fixture paths reported for human triage without deletion.
- `python3 tools/LuaCorpusExtractor/test_lua_corpus_extractor_v2.py`: 32 passed.
- `python3 tools/test_lua_fixture_metadata.py`: 6 passed.
- Fresh Lua comparison `--enforce`, versions 5.1–5.5: zero mismatches, zero
  one-sided failures, and all 1,254 current both-error signatures unchanged.
- `python3 scripts/lint/test_plan_hygiene.py`: 24 passed.
- `python3 scripts/lint/check-plan-hygiene.py`: passed.
- `python3 scripts/lint/check-tooling-consistency.py`: passed; lifecycle behavior
  passed and Docker no-cache verification was explicitly skipped because Docker
  was unavailable.
- `bash scripts/ci/apply-formatters.sh`: passed.
- `./scripts/build/quick.sh`: passed with zero warnings and zero errors.
- `./scripts/test/quick.sh --no-build` after a forced full test-project rebuild:
  15,238 passed; 0 failed; 0 skipped. The focused Basic-module run passed all
  252 tests after adding explicit cross-script warning-state and host-stream
  ownership regressions.
- `git diff --check`: passed (Git reported only the existing CRLF normalization
  warning for `.gitignore`).
- The first full pre-commit run failed because its case-insensitive filename
  check rejected edits to the existing legacy issue-audit document and generator,
  although the repository-wide branding check already retained those lowercase
  historical names. The pre-commit check now has an explicit two-path legacy
  allowlist instead of blocking maintenance of those audit artifacts.
- `bash ./scripts/dev/pre-commit.sh`: passed on the final corrected staged
  aggregate, including formatting, links, generated audits, branding, namespace,
  shell, tooling, PLAN, skill-index, and Liquid checks.

Independent aggregate review, commit/push, PR CI, and remote review resolution
are recorded after those gates complete.
