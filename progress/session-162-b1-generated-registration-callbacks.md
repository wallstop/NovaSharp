# Session 162: B1 Generated Registration Callbacks

Date: 2026-07-05

## Summary

- Added the first public facade host-callback path:
  - `LuaCallback(LuaContext, ReadOnlySpan<LuaValue>)`.
  - `LuaContext.Engine`.
  - `LuaEngine.CreateCallback(...)`.
- Wired generated `[LuaObject]` companion partials to a public `__NovaSharpGeneratedRegister(...)` helper that creates an object table, installs generated method callbacks, calls the enum-table helper, and assigns the object table into a destination table under the `[LuaObject]` name.
- Advanced generated dispatch from inert switch labels to direct sync-method calls with typed argument unpacking for supported primitive/facade types and LuaValue return wrapping.
- Made referenced enum tables runtime-visible through generated registration, closing the B1 enum auto-exposure task.
- Hardened generated code after adversarial review:
  - Normal callback conversion/arity/type errors are normalized to `ScriptRuntimeException` so Lua `pcall` contains them.
  - C# keyword method identifiers are escaped in generated member access.
  - Unsigned integer returns use integer Lua values when they fit in `long` and fall back to numeric values only past the signed range.
  - Function, table, and coroutine facade return values round-trip through generated callbacks.
  - The analyzer now reports `[LuaObject]`-typed member signatures as unsupported until object adapter conversion exists.
  - The generated manifest now lists only generated callback members, so properties/fields are not advertised as registered callbacks before property binding lands.

## Validation

- `dotnet build src/interop/WallstopStudios.NovaSharp.Interop.Generator/WallstopStudios.NovaSharp.Interop.Generator.csproj --no-restore` completed with exit code 0.
- `dotnet build src/runtime/WallstopStudios.NovaSharp.Interpreter/WallstopStudios.NovaSharp.Interpreter.csproj --no-restore` completed with exit code 0.
- `./scripts/test/quick.sh --full -c LuaInteropGeneratorTUnitTests` completed with exit code 0: 15 tests passed, 0 failed.
- `./scripts/test/quick.sh --full -c LuaInteropAnalyzerTUnitTests` completed with exit code 0: 31 tests passed, 0 failed.
- `./scripts/test/quick.sh --full -c NovaSharpFacadeSmokeTUnitTests` completed with exit code 0: 51 tests passed, 0 failed.
- `./scripts/build/quick.sh` completed with exit code 0.
- `dotnet tool restore` completed with exit code 0.
- `dotnet tool run csharpier format .` completed with exit code 0.
- `git diff --check` completed with exit code 0.
- `./scripts/test/quick.sh` completed with exit code 0: 14,898 tests passed, 0 failed, 0 skipped.
- `bash ./scripts/dev/pre-commit.sh` completed with exit code 0.

## Residual Risk

- Property and field binding are still not generated; annotated properties remain accepted by the analyzer but are not part of the generated callback manifest until a real property-access design lands.
- Async generated members still intentionally report `NS0005`; async suspension markers remain pending.
- The public facade callback path currently materializes a pooled `LuaValue[]` for non-empty callback arguments while the VM still stores `DynValue` internally; A5/span-native callbacks are still needed for the final zero-allocation interop target.
