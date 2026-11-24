# Session 196 — Lua runtime parity

The delivery branch started at `b4e777cc`. The session started at 21:36:54 UTC
on November 24, 2026, with a hard stop at 00:36:54 UTC on November 25. GitHub
showed no open pull requests, including drafts or dependency updates. Tests,
Benchmarks, CSharpier, and Pages deployment had succeeded on main.

- Assignment converts internal void to explicit nil at the shared store
  boundary. The base regression dropped the argument slot for zero-result
  assignments. The fixture also exercises a captured local, a global, a table
  write, a `__newindex` handler, left-to-right RHS evaluation, and adjusted return lists.
  [#172](https://github.com/Ambiguous-Interactive/NovaSharp/issues/172)
- Greater-than comparisons evaluate side-effecting operands in source order
  and compare the reversed values. Direct left locals keep the reference
  register read timing. The fixture checks calls that fail, NaN, and metamethod
  dispatch on Lua 5.1–5.5.
  [#181](https://github.com/Ambiguous-Interactive/NovaSharp/issues/181)
- Lua 5.1 numeric coercions use the prefix before NUL. Lua 5.2–5.5 reject a NUL
  anywhere in the input. The paired regressions exercise unary and binary
  arithmetic, numeric loop controls, both `tonumber` forms, and the integer
  coercions used by bitwise operations and `math.tointeger`.
  [#189](https://github.com/Ambiguous-Interactive/NovaSharp/issues/189)
- Floating `string.format` conversions match the reference infinity
  spelling, sign, case, alignment, and space padding.
  [#190](https://github.com/Ambiguous-Interactive/NovaSharp/issues/190)

## Issue triage

The paginated inventory returned 50 issues. The next page was empty. Gameplay
and Lua behavior were prioritized first, followed by performance, validation,
and tooling. The session priority order was 168, 174, 169, 172, 181, 176, 189,
162, 179, 178, 186, 185, 175, 190, 130, 125, 124, 159, 138, 122, 188, 108, 95,
93, 92, 118, 119, 113, 114, 135, 94, 165, 154, 98, 99, 105, 87, 88, 104, 103,
106, 84, 102, 85, 121, 184, 183, 182, 163, and 157. The selected runtime
fixes fit one parity PR. The older local guidance draft remains tracked in
[#163](https://github.com/Ambiguous-Interactive/NovaSharp/issues/163).

Reproductions from this session cover direct local reads in forward
comparisons, versionless numeric callback arguments, finite floating-point
formatting, parallel assignment ordering and value loss, and Lua 5.1 NaN
payload grammar.
[#191](https://github.com/Ambiguous-Interactive/NovaSharp/issues/191)
[#192](https://github.com/Ambiguous-Interactive/NovaSharp/issues/192)
[#193](https://github.com/Ambiguous-Interactive/NovaSharp/issues/193)
[#194](https://github.com/Ambiguous-Interactive/NovaSharp/issues/194)
[#195](https://github.com/Ambiguous-Interactive/NovaSharp/issues/195)

## Validation

- Four regression fixtures reproduced failures on the base and passed in the
  candidate and matching PUC Lua 5.1–5.5. The NaN control passed on both
  NovaSharp revisions. Independent review added probes for mutable locals,
  comparison metamethods, numeric input variants, malformed format digits,
  and the legacy finite and NaN flag handling.
- Comparison disassembly showed unchanged dispatch for local and literal
  cases. Comparisons with two evaluated operands add one `SWAP`. An
  alternating 500,000-iteration CPU-time comparison resolved no difference
  at its 10 ms timer resolution.
- The quick build completed without warnings or errors. All 15,845 quick tests
  passed. The corpus extractor discovered the same 2,021 generated entries
  and the harness discovers the five added hand-authored fixtures on disk.
- The enforced full corpus passed for 6,076 fixture and profile combinations
  with PUC Lua 5.1.5, 5.2.4, 5.3.6, 5.4.7, and 5.5.0. The comparison used a
  writable working directory with the hosted path to avoid the existing
  error-normalization problem. [#165](https://github.com/Ambiguous-Interactive/NovaSharp/issues/165)
- CSharpier, the Markdown checks, and the full pre-commit hook succeeded.
  The hook skipped its Docker-specific no-cache check because Docker was
  unavailable.
- Hosted lint found the Copilot policy mirror was stale. The mirror now
  matches `AGENTS.md`, and the repository indexer suite checks both copies.
- The integer helper now rejects NUL directly for Lua 5.3+ operations. The
  mixed-profile regression sets a Lua 5.1 host default inside an isolated scope
  before exercising explicit script profiles.
- The Windows comparison artifact identified the reference Lua C89 formatter
  diagnostic for unsupported hex modifiers. Stock Lua 5.3.6, 5.4.7, and 5.5.0
  built with `LUA_USE_C89` reproduced the failure and passed the revised
  fixture. TUnit still requires hex modifier support in NovaSharp from Lua 5.2.
