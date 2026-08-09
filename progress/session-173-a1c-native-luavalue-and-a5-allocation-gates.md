# Session 173 — Native LuaValue and A5 allocation gates

Date: 2026-08-09

This session completed the A1c value representation conversion and advanced the highest-impact A5 call-path work. It deliberately stops before the struct call-frame rewrite; the remaining measured blockers are recorded in a follow-up issue.

## Delivered

- Replaced the heap `DynValue` wrapper with the root `NovaSharp.LuaValue` readonly struct and removed the transitional facade wrapper.
- Bound payload-bearing values and public facade handles to script lifetime without storing a redundant owner in each scalar value.
- Preserved explicit absence separately from canonical Nil throughout tables, instructions, callbacks, userdata, CLR conversion, debugger paths, and nullable compatibility APIs.
- Preserved the exact shipped root API while moving legacy runtime-only value operations behind internal access for in-repository tooling.
- Made pooled collections allocation-free after warmup and replaced recursive Lua argument slices with stack windows.
- Reworked frame locals into inline `ValueSlot` structs with lazy `UpvalueCell` allocation only when a local is captured. Escaped `<close>` values retain cell identity and are closed exactly once.
- Consolidated Lua numeral parsing and corrected integer coercion, signed/hex-float parsing, IEEE rounding/subnormal behavior, file numeric reads, `debug.getuservalue`/`setuservalue`, and `string.unpack` position semantics against reference Lua 5.1-5.5.
- Migrated all 32 registered Math functions to stack-only callback argument views while retaining public legacy methods and reflection metadata. Built-in registration deterministically prefers the view overload; external module behavior is unchanged.

## Measured results

| Gate | Before | Current | Result |
| --- | ---: | ---: | --- |
| `fib(30)` allocation | 2,132,514,592 B/op | 192 B/op | A1 allocation gate met |
| NumericLoops allocation | 2,844,016 B/op Phase A0; 1,248,000 B/op immediately before Math migration | 0 B/op | A1 allocation gate met |
| NumericLoops mean | 1.956 ms immediately before Math migration | 1.856 ms | 5.1% faster |
| One-argument/one-result host call | not previously closed | 479.8 ns / 0 B | A5 allocation gate met |
| Lua-to-CLR interop | 475.65 ns / 488 B Phase A0 | 540.11 ns / 0 B | beats same-run NLua 603.53 ns / 504 B |

The allocation work exposes the next bottleneck cleanly. Same-run pure-Lua call speed remains far outside the A5 exit target:

| Scenario | NovaSharp | NLua | Ratio |
| --- | ---: | ---: | ---: |
| `fib(30)` | 1,514.76 ms / 192 B | 73.18 ms / 144 B | 20.70x |
| Tower of Hanoi | 20.264 ms / 168 B | 0.9998 ms / 24 B | 20.27x |

Other open A5 measurements are `new Script()` at 319,436 B (target <100 KiB), fixed resume-3 at 592 B, and coroutine ping-pong at 469.1 us / 242,464 B.

## Verification

- Full solution builds completed with 0 warnings and 0 errors.
- Full TUnit suite: 15,223/15,223 passed.
- Math comparison corpus: 296 fixture executions across Lua 5.1-5.5, 0 mismatches and 0 missing outputs.
- Parser/debug/string changes were differentially checked against the applicable reference Lua 5.1-5.5 executables, including exact stdout/stderr for the changed fixtures.
- `FibonacciRecursive` and NumericLoops allocation gates pass on all applicable compatibility versions.
- VM allocation lint, strict Lua-number lint, repository formatting, diff checks, and full pre-commit pass.
- Unity package generation and the public facade/Hardwire external-consumer probes passed during the native-value conversion; a real Unity Editor IL2CPP player build was unavailable locally.
- Multiple independent adversarial review rounds ended with APPROVE and zero actionable findings.

## Follow-up

The next checkpoint should test the measured hypothesis that converting `CallStackItem` to an inline struct stored directly in `FastStack` and removing synchronized `CallStackItemPool` rent/return work improves recursive-call throughput. The design must use explicit ref-based frame operations, reacquire refs after any stack growth, and preserve exact-once nested resource cleanup. Remeasure recursive-call ratios, script construction, and coroutine create/resume immediately afterward: replacing 64 frame references with 64 large inline structs may improve calls while worsening the memory gates. Only then should the next residual target be selected.
