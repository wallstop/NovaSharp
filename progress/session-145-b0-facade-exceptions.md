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
- `./scripts/test/quick.sh --full -c NovaSharpFacadeExceptionTUnitTests` passed: 11 tests, 0 failures.
- `./scripts/test/quick.sh --full -c NovaSharpFacadeSmokeTUnitTests` passed: 40 tests, 0 failures.
- `./scripts/test/quick.sh --full -c SandboxInstructionLimitTUnitTests` passed: 28 tests, 0 failures. The parallel build emitted one transient MSB3026 copy retry warning and then succeeded.
- `./scripts/test/quick.sh --full -c SandboxAccessRestrictionTUnitTests` passed: 37 tests, 0 failures.
- `./scripts/build/quick.sh --all` passed.
- `./scripts/test/quick.sh` passed: 14,580 tests, 0 failures.

## Open Work

- Phase B0 still needs the full 5% facade `Run`/`Call` overhead exit criterion closed against the A0 scoreboard before the phase can be marked complete.
