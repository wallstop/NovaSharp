# Runtime Research Gates

These are unresolved architectural constraints, not selected implementation
commitments. `PLAN.md` links them only from the milestone that can authorize a
study; experiments and decisions belong in `progress/` sessions and issues.

## Stackless/fuel execution model

Evaluate a reified, steppable executor jointly with the gated A8 VM study. The
candidate must show whether it can preempt native callbacks, cancel by dropping
execution state, schedule tasklets, and support async hosting without weakening
Lua behavior, sandbox limits, or Unity compatibility. Reject it if measured
dispatch overhead or rewrite risk outweighs those capabilities.

## Host-GC and string fidelity

Lua weak tables and version-specific `__gc` behavior must be designed explicitly
over the managed host GC; silent omission is not acceptable. Evaluate weak
reference/ephemeron storage and a deterministic finalization queue against Lua
5.1–5.5. Strings must remain byte-faithful at Lua boundaries rather than inheriting
UTF-16 semantics from the CLR.

## Machine-readable API authority

Evaluate one deterministic API description as the source for generated interop
bindings, LuaLS/EmmyLua stubs, public documentation, and debugger metadata. Adopt
it only if generation proves parity and eliminates duplicated metadata without
adding runtime reflection or harming trimmed/AOT builds.
