# Session 186: Pre-5.3 numeral/for-loop parity and benchmark-gate reliability

Branch: `fix/pre53-parity-and-ci-reliability` (single PR, off `origin/main`).

## Scope

Goal-directed session: address 2+ open issues, keep CI time flat or lower,
seed the under-2-hour session-length guidance into the agentic docs, and land
one green PR.

## Issue fixes

### #134 — for-loop control validation order (fixed)

- Reference 5.1/5.2 `FORPREP` converts init → limit → step, so the *initial
  value* error wins when several controls are invalid; 5.3+ validate limit →
  step → init. `Processor.ExecForPrep` used the 5.3+ order for every profile.
- Fixed with a version-band split plus a shared
  `CoerceForControlOrThrow` helper (also deduplicated the integer-loop limit
  coercion). Verified against `lua5.1`-`lua5.5` directly:
  `for i = {}, {}, {}` → init error on 5.1/5.2, limit error on 5.3+, and
  `for i = {}, 10, {}` → init on 5.1/5.2, step on 5.3+.

### #137 — lexer accepts hex-float syntax on Lua 5.1 (fixed)

- `Lexer.ReadNumberToken` was version-blind. Replaced with reference-faithful
  scanners (`llex.c` `read_numeral` per band):
  - **5.1**: digits+dots, optional `Ee`+one sign, then a trailing
    alnum/underscore run that `strtod` accepts or rejects. `0x1p4` → 16;
    `0x1.5` splits into `0x1` + `.5`; `0x.8` → malformed near `'0x'`;
    `0x8p-3` → malformed near `'0x8p'`; `0xg` → malformed near `'0xg'`.
  - **5.2/5.3**: `0x` prefix switches the exponent mark to `p`/`P`; loop eats
    exponent signs, hex digits, dots; no trailing-garbage fold
    (`0x1g`/`1e5x` split into numeral + name).
  - **5.4/5.5**: same plus trailing-alnum fold (`0x1g`, `1e5x`, `123abc` are
    malformed-number errors).
- The `Lexer` constructor now takes the compatibility version (single
  production caller, `LoaderFast`; JSON converter pins the default profile).
- Numeral validation moved to lex time (`CreateValidatedNumberToken`) so
  `print(0xA.8p0)` reports `malformed number near '.8p0'` like reference
  instead of a downstream parser error.
- Leading-dot numerals keep their raw text (`.5e` not `0.5e`) — this also
  fixes the malformed-numeral error text half of #138 item 1 for every
  version.

### #135 — benchmark aggregate intermittently red (RCA + fix)

- RCA with the run artifacts (`33724727630`, `33818054037`): the downloads
  **succeed** (the `digest-mismatch: error` log lines appear even when the
  recorded and downloaded digests are byte-identical; the download steps
  conclude `success`). The failing step is `Fail when benchmark delta gates
  failed`: a Phase A0 NLua-ratio gate.
- The checked-in baseline (`progress/benchmarks/phase-a0-scoreboard-baseline.json`)
  had been captured from a run where NovaSharp's compile P95s sat in the
  noise-fast tail (`TowerOfHanoi` Compile P95 0.201 ms vs 0.439/0.470 ms in
  sibling runs of the same commit). Any subsequent normal run then tripped
  `+147%`/`+101%` ratio "regressions" on identical code. NovaSharp compile
  P95 varies ~2.3x run-to-run on hosted runners while NLua stays flat, so a
  noise-low baseline makes the gate chronically red.
- Fix: re-captured the baseline from main's latest run (`33818054037`,
  slow-tail sample). Validated red-to-green: 0 gate failures against both
  that run's and the green run's (`33809928739`) artifacts, while a synthetic
  NovaSharp-only 3x Execute regression still fails the gate (2 failures) and
  a uniform all-runtimes slowdown correctly passes.
- Residual: a future artifact-service digest failure remains possible but was
  not the observed cause; issue comments cover this.

### #138 (partial, commented)

- Item 1 (malformed-numeral error text quoting raw source) is fixed by the
  lexer work above; items 2 (`%s` + `__tostring`) and 3 (`'10' + 1` float
  promotion on 5.3) remain open on the issue.

## Session-length guidance (<2h)

- `GOAL.md` Work Style, `AGENTS.md` Critical Rules, `.llm/context.md`, and
  the plan-maintenance skill (new "Session scoping" section) now target
  sub-2-hour agent sessions scoped to one shippable PR slice.

## Verification

- `./scripts/build/quick.sh --all` green; full TUnit suite
  15,714/15,714 passed.
- Local enforced comparison matrix for 5.1-5.5 (fast runner + compare +
  ratchet): all `[OK]`; new both-error entries (known chunk-prefix divergence
  class) merged into `docs/testing/lua-error-ratchet.json`.
- New/curated fixtures: for-loop validation order (2), Lua 5.1 hex-float
  rejection (5), raw-text malformed numerals (3); HexFloats1-3 and
  `HexFloatLiteralParsesToExpectedNumber` re-scoped to 5.2+ (fixtures were
  already curated 5.2+; the C# attributes were wrong).
- Phase A0 baseline gates re-validated against two historical runs plus a
  synthetic regression (gate fires) as documented above.

## Follow-ups

- The P95-ratio gate for compile operations remains sensitive to runner
  variance; a multi-run median baseline would harden it (bigger tooling
  change, deferred).
- #124-class error-prefix divergence (`file:line:` vs NovaSharp's decorated
  form) is the reason new error fixtures land as both-error ratchet entries.

## Post-merge gate calibration (adversarial review follow-ups)

The independent adversarial review and the first PR CI runs surfaced two
residual defects, both fixed in this PR:

1. **`Latest` alias leaked into the new version bands**: the default profile
   (`LuaCompatibilityVersion.Latest = 0`) fell into the `<= Lua52` for-loop
   order branch and missed 5.4 numeral scanning. `ExecForPrep` and the
   `Lexer` constructor now `LuaVersionDefaults.Resolve` the alias; the two
   Lua 5.1-era TestMore TAP suites that had been passing against the old
   accidental pre-5.4 default (zero-step tolerance, "must be a number" text)
   are pinned to Lua 5.1 in `TapSuiteCatalog` like the existing overrides;
   default-profile regression tests pin the 5.4 behaviors.
2. **Gate calibration from observed CI noise** (run 35410624316):
   - fib Execute B/op 168 → 24,792 B is attribution noise in a 2.3 s op
     (locally the same benchmark allocates 168/192 B; compile B/op unchanged),
     so operations with a baseline mean ≥ 1 s get a 32 KiB allocation
     allowance; short operations keep the byte-exact gate.
   - NBody Compile NLua P95 ratio +103.77% is the documented compile-P95
     jitter class (2.0-3.7x swings on identical code, mean ≤ 1.5x), so
     Compile/Cached Compile rows gate on the mean ratio only.

Validated: 0 gate failures against three historical artifact sets
(33809928739, 33818054037, 35410624316) while synthetic NovaSharp-only 3x
regressions still fail (Execute via mean+P95, Compile via mean).
