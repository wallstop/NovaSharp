# Session 172 — GitHub Pages CI recovery and fixture-corpus integrity

Date: 2026-07-29

Closes [#90](https://github.com/wallstop/NovaSharp/issues/90) and
[#91](https://github.com/wallstop/NovaSharp/issues/91); makes the `pages build and deployment`
workflow green on `main` for the first time since it was enabled.

______________________________________________________________________

## 1. `main` CI was red and had never been green

`Tests`, `CSharpier`, and `Benchmarks` were all passing on `abba1f3f`. `pages build and deployment`
was not — and querying the workflow's whole history showed **all 39 runs since 2026-07-01 failed or
were cancelled**. The published site was 404 at both `/` and `/benchmarks/`; nothing had ever
deployed.

Job logs are not readable without repository write access, and the failing check run's annotation is
truncated at 4 KiB, cutting off mid-render. So the build was reproduced locally instead: Ruby 3.1
plus `github-pages` **v232** (the exact version the annotation names, pinning jekyll 3.10.0), run
against `git archive HEAD` so the dirty working tree could not contribute:

```
Liquid Exception: Liquid syntax error (line 965): Variable '{{n=2}' was not properly
terminated with regexp: /\}\}/ in docs/lua-spec/lua-5.1-spec.md
```

### Root cause

GitHub Pages serves this repository from the branch root through the `github-pages` gem, whose
plugin set includes **`jekyll-optional-front-matter`**. That plugin promotes *every* Markdown file in
the repository to a Jekyll page, so every Markdown file is parsed by Liquid **before** Markdown
conversion — code fences included. Two Lua examples wrote a nested table constructor without inner
spaces:

| File | Line | Text |
| ---------------------------- | ---- | -------------------------------- |
| `docs/lua-spec/lua-5.1-spec.md` | 965 | `local t = {{n=2}, {n=1}, {n=3}}` |
| `docs/lua-spec/lua-5.2-spec.md` | 1671 | `local t = {{x=3}, {x=1}, {x=2}}` |

Liquid reads `{{n=2}` as a variable opened with `{{` and closed with a single `}`, and
`Liquid::BlockBody#create_variable` raises. One such sequence anywhere in the repository takes the
entire site down.

### Fix

Both samples now read `local t = { {n=2}, {n=1}, {n=3} }` — identical Lua, no `{{` token. Verified by
a real build: **exit 0 in 3.4 s, zero Liquid warnings**, 3,732 files emitted including `index.html`
and every rendered doc page.

### Eliminating the failure mode, not the instance

Two spaced-out braces do not stop the next doc from doing the same thing, and no other CI leg reads
Markdown as Liquid — the Pages workflow is the only signal, and it only fires *after* merge to
`main`. `scripts/ci/check_jekyll_liquid.py` closes that gap. It enumerates the Markdown files Jekyll
would render (tracked `*.md`/`*.markdown`, excluding the `.`/`_` prefixes Jekyll's `EntryFilter`
drops) and reproduces the three constructs Liquid treats as fatal:

- `{{` not terminated by `}}`
- `{%` not terminated by `%}`
- `{% name %}` where `name` is not a tag available on GitHub Pages

It runs **repository-wide, not diff-scoped**, from `scripts/ci/check-markdown.sh` (which CI already
invokes) and from `scripts/dev/pre-commit.sh` — because a change that touches no Markdown at all can
still be the one that gets blamed when `main` goes red.

**The guard was validated against the real thing.** All 18 cases in
`scripts/ci/test_check_jekyll_liquid.py` were run through an actual `github-pages` v232 build, one
build per case, comparing guard verdict against Jekyll's exit code. The first pass found a
**disagreement**, and Jekyll was right:

> `{%- raw -%}...{%- endraw -%}` — guard said pass, Jekyll said
> `Liquid syntax error: 'raw' tag was never closed`.

`Liquid::Raw#parse` closes its region with its own regex,
`/\A(.*)\{\%\s*(\w+)\s*(.*)?\%\}\z/om`, which has no whitespace-control branch — unlike the tag
parsing in `Liquid::BlockBody`. So `{%- raw -%}` opens a raw region but `{%- endraw -%}` does not
close one. The guard now matches that asymmetry, and the case is pinned as a test. Final
differential: **18/18 agree, 0 mismatches.**

Writing the guard from the documentation alone would have shipped a false negative in the one
direction that matters.

### The guard's first act was to reject this very document

With the guard wired in, `pre-commit` failed on 11 locations — in `PLAN.md`, `scripts/ci/README.md`,
and the progress note describing the fix. Every one was real: writing *about* an unterminated `{{`
produces an unterminated `{{`. The site would have gone red again on the commit that fixed it.

That is the signal that the *scan surface* was wrong, not the guard. Pages was publishing all 257
Markdown files in the repository — 171 session notes, the roadmap, agent instructions, per-directory
tooling READMEs — none of which are reader documentation, and all of which will keep quoting Lua
table constructors and Liquid delimiters forever. Escaping them one at a time is an unbounded tax.

So `_config.yml` now scopes the site to what it is actually for: `README.md` (which
`jekyll-readme-index` turns into the home page) and `docs/`. `progress/`, `scripts/`, `src/`,
`tools/`, `PLAN.md`, `AGENTS.md`, `CLAUDE.md`, and build configuration are excluded. Nothing
regresses — the site had never built, so every published page is new. The guard reads
`_config.yml`'s `exclude` list, so scan set and published set cannot drift; a missing or unreadable
config falls back to scanning **everything**, since publishing everything is the state that broke the
site.

Result: the scan went from 257 files to 57, internal notes are free to quote whatever they need, and
`docs/` — where the original bug lived — is still guarded.

Verified against a real build of the exact staged file set: **exit 0 in 1.8 s, 0 Liquid warnings**,
128 files, 56 rendered pages, and `progress/`, `src/`, `scripts/`, `tools/`, `PLAN.html`,
`AGENTS.html`, `CLAUDE.html` all absent from the output. 56 of 57 published Markdown files render;
the one that does not is `docs/Contributing.md`, which `jekyll-optional-front-matter` deliberately
skips along with the other repository meta-documents.

______________________________________________________________________

## 2. The corpus extractor destroyed curated metadata (#90)

`@lua-versions`, `@novasharp-only`, and `@expects-error` are decided by a human against reference Lua
and are the only three keys `compare-lua-outputs.py` reads. The extractor recomputed all three from
source heuristics on every run. Measured by regenerating into a scratch directory and diffing
against the committed corpus:

| Damage | Count |
| ---------------------------------- | ----- |
| `@lua-versions` values rewritten | 384 |
| `@novasharp-only` flipped (**both directions**) | 11 |
| `@expects-error` flipped | 12 |
| Curated comment lines deleted | 1,256 |

`MyObject/IndexSetDoesNotWrackStack.lua` lost `@novasharp-only: true` (silently re-enabling a
comparison against reference Lua whose table iteration order differs) and
`ParserTUnitTests/UnicodeEscapeSequenceIsDecoded.lua` had `5.3+` widened to `5.1+` (testing `\u{...}`
against interpreters that predate it).

### Fix

For a fixture that already exists on disk, the committed header now wins: only `@source`, `@test`,
and the body are refreshed, and curated lines survive byte-for-byte, including unrecognised `@keys`
for forward compatibility. `--refresh-metadata` is the deliberate escape hatch, and it prints a
warning that every changed fixture must be re-audited.

The pass runs before *both* `write_snippets` and `write_manifest`, so the files on disk and the
manifest can no longer disagree, and it prints every curated value it kept against what the
heuristics would have computed — **72 divergences**, which is the audit trail that did not exist
before.

### Two further defects found by measuring rather than reviewing

**`@source` used OS-native path separators.** 1,820 committed fixtures held `src\tests\...` and 239
held `src/tests/...`, so regenerating on Linux rewrote the first set and regenerating on Windows
rewrote the second. Every regeneration was a ~1,800-file diff in which a real metadata change was
invisible. `@source` is now always POSIX, and the committed corpus was normalised once.

**Fixture bodies that begin with a Lua comment were absorbed into the header.** "Leading `--` lines"
is not a sufficient definition of the header when the snippet itself starts with `--`.
`SimpleTUnitTests/Factorial.lua` gained a second copy of `-- defines a factorial function`, and
would have gained one more on every subsequent run — unbounded growth. The extracted snippet is the
ground truth, so any tail of the parsed header matching the snippet's own leading comments is now
returned to the body.

Both were found by running the extractor twice and diffing, not by reading the diff of run one.

### Manifest/header agreement is now enforced

`novasharp_only` for a NovaSharp-only fixture is recoverable from its header; the version list is
not, because `version_string` collapses to the literal `novasharp-only`. The manifest reported
versions anyway — with no consistency: 335 such entries listed 5 versions, 62 listed 1, 39 listed 3,
2 listed none. `compatible_versions` now returns `[]` when `novasharp_only`, which is both honest
(these fixtures never run against a reference interpreter) and lossless, so header → manifest →
header round-trips.

`tools/test_lua_fixture_metadata.py` gained
`test_manifest_agrees_with_every_fixture_header`, which walked in **red at 463 drifted fixtures** and
is green now. Regeneration is verified **idempotent across three consecutive runs**, byte-for-byte,
fixtures and manifest alike.

______________________________________________________________________

## 3. The 144 unextracted fixtures (#91)

144 fixtures existed as C# tests but had never been extracted, so they had never been compared
against reference Lua. They are now in the corpus (2,142 → 2,286) and were run against all five
reference interpreters. Exactly **two** diverged, matching session 171's prediction:

| Fixture | Divergence | Resolution |
| ----------------------------------- | ------------------- | ----------------- |
| `DeepBoundedRecursionSucceedsUnderDefaultCeiling.lua` | `lua5.1` raises `stack overflow` at `sum(20000)`; 5.2-5.5 and NovaSharp do not | `@lua-versions: 5.2+` |
| `RepeatedCaughtStackOverflowsKeepVmHealthy.lua` | NovaSharp exceeds the harness's 5 s budget on 5.1-5.3 but not 5.4-5.5 | `@novasharp-only: true` |

A third fixture, `ClrReentrantOverflowCaughtByPcallLeavesNoOrphanedValueSlots.lua`, was passing only
vacuously: it calls injected host functions (`reenter`, `probe_before`, `probe_after`), so both
engines raise "attempt to call a nil value" and the comparison scored a both-error match. It is now
`@novasharp-only: true` with the injected names recorded, matching the existing convention for
`InteropMetaEquality`.

Recursion depth is implementation-defined in Lua, and NovaSharp's ceiling is deliberately
configurable (`MaxVmValueStackSize` / `MaxVmCallStackSize`, default 1,000,000, overflowing near 250k
frames — between reference 5.1 and 5.4). Comparing a depth-limit fixture against reference Lua
measures each engine's own limit, not Lua semantics. Every one of these behaviours remains covered by
`VmStackCeilingTUnitTests` on all five compatibility versions; only the reference comparison is
skipped.

### Session 171 asked for fixture-level timeout metadata. It is not needed.

The only fixture that exceeded the 5 s budget did so because it drives NovaSharp's configurable
ceiling 50 times, which is not a parity question. Marking it `@novasharp-only` removes the timeout
*and* the non-determinism (it passed on 5.4/5.5 and failed on 5.1-5.3 in the same run) with no new
mechanism — and `CLAUDE.md` restricts fixture metadata to exactly three keys, so a fourth was never
an option. Recorded here so the follow-up is closed with evidence rather than left open.

______________________________________________________________________

## Verification

| Check | Result |
| ------------------------------------------ | ------------------------------------------------ |
| `github-pages` v232 build of `HEAD` tree | exit 0, 3.4 s, 0 Liquid warnings |
| Guard vs. real Jekyll, 18 cases | 18/18 agree, 0 mismatches |
| `scripts/ci/test_check_jekyll_liquid.py` | 5 tests green |
| `check_jekyll_liquid.py` repo-wide | 257 rendered files, 0 findings |
| `tools/LuaCorpusExtractor/test_lua_corpus_extractor_v2.py` | 16 tests green |
| `tools/test_lua_fixture_metadata.py` | 4 tests green (was 3, one red) |
| Extractor idempotence | 3 consecutive runs byte-identical |
| `compare-lua-outputs.py` 5.1-5.4 `--enforce` | **0 mismatch, 0 lua_only, 0 nova_only** |
| `compare-lua-outputs.py` 5.5 `--monitor` | 0 mismatch |
| Both-error ratchet | 1,255 entries, 0 new / 0 changed / 0 missing |

Match counts per version: 931 / 701 / 875 / 936 / 972.

______________________________________________________________________

## What this unblocks, and what A1c actually costs

This session deliberately did not touch [#96](https://github.com/wallstop/NovaSharp/issues/96)
(`fib(30)` allocating 2.1 GB/op) or [#95](https://github.com/wallstop/NovaSharp/issues/95), which are
the highest gameplay-impact issues open. Their fix is Phase A1c — `DynValue` class → `readonly struct
LuaValue` — and PLAN.md's own methodology forbids starting it here: *"No phase merges with a red
fixture,"* and the corpus was not trustworthy. 463 fixtures had drifted from their manifest, 144 had
never been compared against reference Lua at all, and `main`'s Pages leg had never been green. Those
are now fixed, so A1c can be judged against a harness that means something. #96 got a written
diagnosis against the committed A0 numbers instead of a partial fix.

Measured blast radius, so the next session can size it rather than estimate:

| Signal | Count |
| ------------------------------------------ | ------ |
| `DynValue` references across `src/` | 13,217 |
| Files mentioning `DynValue` (whole repo) | 366 |
| Files mentioning `DynValue` (runtime only) | 136 |
| `DynValue.NewNumber` call sites | 1,412 |
| `DynValue.Nil` / `DynValue.Void` references | 552 / 200 |
| `?? DynValue.Nil` (the null-vs-Nil hazard PLAN.md flags) | 82 |
| `DynValue.cs` itself | 1,892 lines |

The work is concentrated rather than uniform — `Script.cs` (470), `ProcessorInstructionLoop.cs` (213),
`CompiledScript.cs` (201), `DynValue.cs` (199), and `ScriptExecutionContext.cs` (176) hold a large
share of the runtime references. That is consistent with PLAN.md's compiler-error-driven plan: the
conversion is mechanical in most of the 366 files and genuinely hard in about five.

The 82 `?? DynValue.Nil` sites are the ones PLAN.md says must be **audited manually, not by regex**,
because `default(LuaValue) == Nil` silently changes what a null coalesce means. That count is the
honest measure of the phase's risk, not its file count.

______________________________________________________________________

## Review round on PR #97

Copilot could not review this PR at all — both attempts returned "exceeds the maximum number of
files (300)". The diff is ~1,890 files, of which 1,879 are the regenerated corpus. Getting under the
limit would mean splitting the regeneration into its own PR. So Bugbot was the only automated
reviewer, and the hand-written surface got an adversarial pass on the main thread instead.

### Bugbot found one issue, and it was the sharpest one available

`tools/LlmSkillIndexer/llm_skill_indexer.py` built `file_path` with `str(Path.relative_to(...))`,
which is OS-native. That value lands in the committed `.llm/skills-index.json` — and **this session is
what made `--check` compare that file byte-for-byte**. So the session converted a cosmetic
inconsistency into a CI-breaking one, in exactly the bug class it was fixing for fixture `@source`
paths, in a file it had just made fail-closed.

Sweeping the class across all 73 `relative_to` call sites in `tools/` and `scripts/` found **one more
live instance Bugbot did not flag**: `lua_corpus_extractor.py` — the v1 extractor, still pointed at by
`scripts/tests/run-lua-corpus.sh:99` — carries the identical line v2 was fixed for. Running it would
rewrite `@source` on every fixture and undo this session's normalisation. Everything else was already
`as_posix()`, kept as a `Path` for `.parts` comparison, or human-facing output only.

Both fixes are **Windows-only in effect** — on Linux `str(Path)` and `as_posix()` are the same string —
and the `lint` job that runs the indexer check is `ubuntu-latest` only. So neither the bug nor its new
test will ever fire in CI. The code change is the protection; the test pins intent. Claiming red/green
here would be false.

### Self-review found a false negative in the guard

Jekyll's `markdown_ext` defaults to `markdown,mkdown,mkdn,mkd,md` (jekyll-3.10.0
`configuration.rb:27`), and `jekyll-optional-front-matter` promotes every one to a page. The guard
covered two. A `.mkd`, `.mkdn`, or `.mkdown` file could therefore carry an unterminated delimiter,
take the site down, and **pass the guard silently** — the one failure direction it exists to prevent.

Verified rather than assumed: the same bad snippet was built under each of the five extensions and all
five abort the build. The guard now agrees on all five and reads the set from `_config.yml`'s
`markdown_ext`, the same treatment `exclude` already had. No such file exists today; fixed because a
guard whose value is "no false negatives" cannot have one.

### A third defect, filed rather than fixed

Counting snippets against unique output paths showed 1,965 snippets collapsing onto 1,940 paths. All
25 collisions come from **different source files**: `output_path` keys on the *simple* class name, which
is not unique across the test tree (`ClrToScriptConversionsIntegrationTUnitTests.cs` declares
`class ClrToScriptConversionsTUnitTests`), and `method_snippet_counts` is per-file so no `_1` suffix is
added. `write_snippets` overwrites, `manifest.json` lists both, and 25 tests contribute no
reference-Lua comparison at all.

This does not weaken the new manifest/header agreement test — all 25 duplicated paths happen to carry
identical curated values, so whichever entry wins agrees with the file on disk. But it holds by luck
rather than construction. Filed as #101 rather than fixed here: the 25 recovered snippets would each
need reference-Lua triage, which is #91's work over again and does not belong in an already-large PR.

______________________________________________________________________

## Follow-ups filed

- **[#98](https://github.com/wallstop/NovaSharp/issues/98)** — the batch runner ignores
  `--fixtures-dir`, writing outputs flat while the reference side nests them. A run over a
  non-canonical fixtures directory compares **nothing** and still prints `[OK] All comparable fixtures
  match`, because `lua_only` is not a failure without `--enforce`. This wasted a full triage cycle
  here. The more important half of the fix is making a zero-comparable-pair run fail loudly, which
  covers any future layout mismatch rather than this one.
- **[#99](https://github.com/wallstop/NovaSharp/issues/99)** — the injected-variable heuristic reads
  raw snippet text *including comments* and misses host functions registered as CLR callbacks. Both
  directions have already produced wrong metadata: `ClrReentrantOverflowCaughtByPcall...` shipped as
  comparable, and session 171's `SparseAndDenseIntegerKeysCoexist.lua` was wrongly excluded.
- **[#100](https://github.com/wallstop/NovaSharp/issues/100)** — 346 committed fixtures are no longer
  produced by extraction (renamed or deleted tests). They still execute, because the harness walks the
  corpus directory rather than the manifest, but nothing reports them as orphaned.
- **[#101](https://github.com/wallstop/NovaSharp/issues/101)** — 25 snippets are silently overwritten
  by fixture-path collisions on the simple class name.

Pages publishing scope beyond "green" belongs with #85; `_config.yml` makes the current scope
intentional and the guard makes it safe meanwhile.
