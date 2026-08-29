# Sandbox Threat Model

This document is authoritative for NovaSharp's untrusted-mod security backlog.
`PLAN.md` selects the current A3/B4 slice; implementation status and validation
belong in issues and `progress/` sessions.

## Security invariants

An untrusted script must not:

- terminate or permanently corrupt the host process;
- consume unbounded CPU, stack, memory, or wall-clock time;
- obtain CLR reflection, raw host objects, files, network, or update channels
  without an explicit capability;
- leak host exception types, stack traces, pointers, or mutable built-in
  metatables; or
- claim deterministic replay while exposing platform-, hash-, GC-, or
  floating-point-dependent state.

Limit failures must surface as catchable Lua errors when recovery is safe, and the
engine must remain reusable after a contained violation.

## Required controls

| Control               | Requirement and acceptance evidence                                                                                                                                                           | Owner |
| --------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----- |
| Pattern steps         | Bound each Lua pattern match independently because VM instruction fuel cannot observe work inside a builtin. Prove adversarial backtracking terminates within the configured budget.          | A3    |
| Call depth            | Reject deep Lua calls, metamethod chains, and protected-call recursion before exhausting the CLR stack. The error must be catchable and deterministic.                                        | A3    |
| Wall-clock watchdog   | Pair deterministic fuel with cancellation/watchdog signaling for long builtins and host callbacks. Never abort Unity objects from a background thread.                                        | A3/B3 |
| Binary chunks         | Reject precompiled/binary chunks by default for untrusted engines, including `load`/`string.dump` round trips. Trusted hosts may opt in explicitly.                                           | A3/B4 |
| CLR confinement       | Expose only wrapped, unforgeable host capabilities. Deny reflection and raw CLR types; make string and standard-library metatables read-only.                                                 | B4    |
| Memory and exceptions | Raise a Lua sandbox error before managed allocation reaches an uncontrolled OOM boundary. Normalize host exceptions without leaking types, stacks, or capabilities.                           | A3/B4 |
| Cooperative yielding  | Add bounded main-thread yield points so long scripts cannot freeze Unity while preserving Unity's main-thread API rules.                                                                      | A3/B3 |
| Deterministic preset  | Define iteration/hash policy, floating-point scope across CoreCLR/Mono/IL2CPP, GC-observable API behavior, and pointer-like `tostring` output. Document any behavior that cannot be portable. | B4    |
| Hot reload            | Reload into a fresh per-mod engine through explicit `save_state`/`load_state` serialization. Invalidate or reject stale closures, coroutines, timers, and subscriptions.                      | B4    |
| Supply chain          | Treat auto-update, side-loading, and transitive dependencies as capabilities. Verify artifact integrity and keep least-privilege defaults visible to the host.                                | B4    |

## Closure gates

- Add negative and boundary tests for every implemented control, including engine
  reuse after denial or exhaustion.
- Run applicable tests across Lua 5.1–5.5 and distinguish intentional host policy
  from Lua language behavior.
- Prove memory, fuel, call-depth, and watchdog limits on CoreCLR and available
  Unity Mono/IL2CPP targets.
- Keep trusted and untrusted defaults explicit in public API documentation and
  samples; no control may depend on a hidden global switch.
- Record accepted limits, platform scope, and unavailable Unity evidence in the
  implementation session and issue rather than in `PLAN.md`.
