# Session 143: B0 Facade First Slice

Date: 2026-07-03

## Summary

- Started Phase B0 by adding root `NovaSharp` facade wrappers over the current VM.
- Added `LuaEngine`, `LuaValue`, `LuaTable`, `LuaFunction`, `LuaCoroutine`, `LuaChunk`, `LuaKind`, `LuaCoroutineState`, `LuaEngineOptions`, `LuaVersion`, `LuaCoreModules`, `LuaSandboxOptions`, and root loader/time/random provider interfaces.
- Wired sync `Run`, `Compile`, fixed-arity and span `Call`, global/table access, table-to-value assignment, and Lua function coroutine create/resume/close paths.
- Kept the public facade contracts in the root `NovaSharp` namespace and adapted them to current VM option/provider types internally.
- Added `RunAsync` as a non-suspending placeholder over the sync path so the public shape exists before the real async/coroutine bridge.
- Added `PublicAPI.Shipped.txt` plus a reflection-backed TUnit baseline check for the root facade surface, public constructors, and the `<40` core type budget.
- Added focused facade smoke coverage across Lua 5.1-5.5, including scalar equality, float/integer subtype handling, table round-trips, and disposed/foreign-engine handle checks.
- Addressed Cursor Bugbot feedback by normalizing execution results to the first scalar value for `Run`, `Call`, compiled chunk execution, and coroutine resume/close paths.

## Validation

- `./scripts/build/quick.sh` exited 0 after the runtime facade changes.
- `./scripts/test/quick.sh --full -c NovaSharpFacadeSmokeTUnitTests` exited 0 with 35 tests passing after the review fix.
- `bash ./scripts/dev/pre-commit.sh` exited 0; only the repo's existing LLM skill metadata warnings remain.
- `./scripts/test/quick.sh` exited 0 with 14,564 tests passing after the review fix.

## Open Work

- Phase B0 remains open: public exception types, hello/per-frame/sandbox samples, and the `Run`/`Call` A0 scoreboard comparison are not complete yet.
- `LuaEngineOptions` is a sealed class for the current netstandard2.1 runtime; the planned record-style options shape can be revisited when the public API is locked for the full B0 milestone.
