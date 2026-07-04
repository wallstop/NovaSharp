# Session 145: B0 Facade Exceptions

Date: 2026-07-04

## Summary

- Continued Phase B0 after the facade samples/overhead slice.
- Added the root facade exception hierarchy:
  - `LuaException`
  - `LuaSyntaxException`
  - `LuaRuntimeException`
  - `LuaSandboxException`
  - `LuaSandboxViolationKind`
- Translated internal `InterpreterException` failures at public facade boundaries while preserving argument, ownership, disposal, and cancellation exceptions as CLR misuse/cancellation failures.
- Preserved interpreter diagnostics through `DecoratedMessage`, `InstructionPointer`, and `CallStack` on `LuaException`.
- Preserved sandbox details through `ViolationKind`, `ConfiguredLimit`, `ActualValue`, `DeniedAccessName`, `IsLimitViolation`, and `IsAccessDenied` on `LuaSandboxException`.
- Added focused facade exception smoke tests covering syntax, runtime, chunk, function call, coroutine, sandbox access, instruction limit, coroutine limit, cancellation, and misuse paths.
- Refreshed `PublicAPI.Shipped.txt` and updated `PLAN.md` to mark the B0 facade type checklist item complete.

## Validation

- `dotnet build src/runtime/WallstopStudios.NovaSharp.Interpreter/WallstopStudios.NovaSharp.Interpreter.csproj -c Release` passed with 0 warnings and 0 errors.
- `./scripts/test/quick.sh --full -c NovaSharpFacadeExceptionTUnitTests` passed: 12 tests, 0 failures after the Cursor follow-up.
- `./scripts/test/quick.sh --full -c NovaSharpFacadeSmokeTUnitTests` passed: 42 tests, 0 failures after the Copilot owner-retention and disposal follow-ups.
- `./scripts/test/quick.sh --full -c SandboxInstructionLimitTUnitTests` passed: 28 tests, 0 failures. The parallel build emitted one transient MSB3026 copy retry warning and then succeeded.
- `./scripts/test/quick.sh --full -c SandboxAccessRestrictionTUnitTests` passed: 37 tests, 0 failures.
- `./scripts/build/quick.sh --all` passed.
- `./scripts/test/quick.sh` passed: 14,583 tests, 0 failures after the Copilot owner-retention and disposal follow-ups.

## Open Work

- Phase B0 still needs the full 5% facade `Run`/`Call` overhead exit criterion closed against the A0 scoreboard before the phase can be marked complete.

## Review Follow-up

- Addressed Cursor feedback on 2026-07-04 by making `LuaSandboxException` snapshot `SandboxViolationException.Details` once and tolerate message-only sandbox exceptions as `Unknown` violations with zero limits.
- Added regression coverage for wrapping a message-only `SandboxViolationException` without losing the original message or raising a secondary CLR exception.
- Addressed Copilot feedback on 2026-07-04 by removing the redundant `GC.SuppressFinalize(this)` call from `LuaEngine.Dispose()`, because `LuaEngine` has no finalizer.
- Addressed Copilot feedback on 2026-07-04 by making scalar `LuaValue` wrappers ownerless while preserving owner binding for tables, functions, threads, userdata, and tuples; tuple decomposition now also drops owners from scalar tuple elements.
- Addressed Copilot feedback on 2026-07-04 by making `LuaValue.AsTuple()` validate its owning engine before materializing tuple elements, matching table/function/coroutine facade handle disposal behavior.
- Addressed Copilot feedback on 2026-07-04 by making `LuaValue.Read<T>()` and `TryRead<T>()` validate disposal for owner-required resource values without changing scalar literal reads.
