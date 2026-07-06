# Session 163: B1 Property Field Bindings

Date: 2026-07-06

## Summary

- Advanced the B1 source-generator MVP by adding live generated property and field binding for supported `[LuaMember]` members.
- Added `LuaTable.SetMetatable(...)` to the root facade so generated bindings can attach metatables directly without depending on the Lua `setmetatable` library being loaded.
- Generated object registration now keeps methods and enum tables as raw object-table entries, while property/field names stay absent and are served by generated `__index` and `__newindex` callbacks.
- Property/field binding uses the existing reflection-free conversion path:
  - reads wrap CLR values through the generated `LuaValue` helpers.
  - writes unpack `LuaValue` arguments through the generated typed reader.
  - enum properties round-trip through generated enum tables.
  - init-only properties, readonly fields, and struct/record-struct members are read-only because writes would not persist safely.
- Added runtime coverage for stale-snapshot regressions: a generated method mutates `Health`, then Lua reads `player.Health` and must observe the updated CLR instance state.
- Added runtime coverage for writable fields, readonly fields, setter conversion errors, and unknown generated-object writes.
- Addressed adversarial review feedback by making the analyzer reject static generated members, indexer properties, and const/static fields that the generator intentionally does not emit.

## Validation

- `./scripts/test/quick.sh --full -c LuaInteropGeneratorTUnitTests` completed with exit code 0: 18 tests passed, 0 failed.
- `./scripts/test/quick.sh --full -c LuaInteropAnalyzerTUnitTests` completed with exit code 0: 34 tests passed, 0 failed.
- `./scripts/test/quick.sh --full -c NovaSharpFacadeSmokeTUnitTests` completed with exit code 0: 51 tests passed, 0 failed.
- `./scripts/build/quick.sh` completed with exit code 0.
- `./scripts/test/quick.sh` completed with exit code 0: 14,904 tests passed, 0 failed, 0 skipped.
- `git diff --check` completed with exit code 0.
- `bash ./scripts/dev/pre-commit.sh` completed with exit code 0 with existing LLM skill metadata warnings.

## Residual Risk

- Async generated members still intentionally report `NS0005`; async suspension markers remain pending.
- EmmyLua/LuaLS stub output remains pending.
- The analyzer package is still compiled and tested directly but is not yet wired into runtime projects as a live analyzer.
- `LuaTable.Get(...)` and `LuaTable.Set(...)` remain raw facade operations; generated property/field binding is guaranteed through normal Lua script access, not host-side raw table reads.
- PR CI and reviewer feedback have not been observed for this branch yet.
