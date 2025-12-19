# Runtime Naming Alignment Audit (2025-11-20)

## Scope & Method

- Focused on `src/runtime/NovaSharp.Interpreter` because the modernization plan calls out runtime naming drift first.
- Searched for non-PascalCase method and field identifiers by running `rg -n -P "^[^\\n]*([A-Za-z0-9]+_[A-Za-z0-9]+)\\s*\\(" src/runtime/NovaSharp.Interpreter -g"*.cs"` together with a variant for field declarations and a follow-up search for multi-line signatures such as `gmatch_aux_2`.
- Manually spot-checked flagged files to separate intentional Lua surface strings (`__ipairs`, `_VERSION`, etc.) from actual C# identifiers that violate our `_camelCase`/`PascalCase` guidance.

## Hotspots

### ErrorHandlingModule helpers

- `pcall_continuation` and `pcall_onerror` in `src/runtime/NovaSharp.Interpreter/CoreLib/ErrorHandlingModule.cs:135-152` are public helpers that do not follow the PascalCase method convention. They are invoked through `CallbackFunction` factories, so renaming requires updating those delegate instantiations plus the NUnit mirrors that assert tuple contents.
- Because they are surfaced via `DynValue.NewCallback`, the current casing leaks into stack traces logged during failures, which keeps the Lua-visible names inconsistent with the rest of the runtime diagnostics.

### KopiLua string library port

- `src/runtime/NovaSharp.Interpreter/LuaPort/KopiLuaStringLib.cs` mirrors the original KopiLua C sources. At least 18 methods (`check_capture`, `match_class`, `max_expand`, `min_expand`, `start_capture`, `end_capture`, `match_capture`, `push_onecapture`, `push_captures`, `str_find`, `str_match`, `gmatch_aux`, `gmatch_aux_2`, `str_gmatch`, `gfind_nodef`, `add_s`, `add_value`, `str_gsub`, `str_format`) and their associated helper structs use snake_case names, and the file defines consts such as `LuaPatternMaxCaptures`.
- These functions are entirely internal but appear prominently in stack traces when Lua pattern matching fails, so inconsistent casing reaches modders.
- Any rename must keep parity with Lua manual sections (§6.4) and ensure we do not regress the TAP/Lua spec mirrors. The safest approach is a mechanical rename supported by unit tests and a diff against the upstream KopiLua reference to ensure we do not break future syncs.

### LuaStateInterop LuaBase

- `src/runtime/NovaSharp.Interpreter/Interop/LuaStateInterop/LuaBase.cs` exposes Lua C API constants (`LuaTypeNil`, `LuaMultipleResults`, etc.) and the helper `LuaQuoteLiteral`. They intentionally mirror the native API, but they violate the stated `_camelCase`/PascalCase rules and currently bypass analyzers because we never enabled IDE1006.
- These members are `protected`, so the inconsistent casing leaks into derived types and any subclass overrides.

### Tooling coverage gaps

- `.editorconfig` only defines naming rules for interfaces, events, consts, tests, etc.; there is no rule that enforces PascalCase for methods or `_camelCase` for private fields, so IDE1006 is not emitted anywhere and the current `docs/audits/naming_audit.log` (59 B) incorrectly reports "All inspected files/types follow PascalCase expectations."
- CI does not run any dedicated audit; contributors rely on manual review, which is how the current drift survived the modernization to `net8.0`.

## Proposed Alignment Plan

1. **Runtime helper renames**
   - ✅ (2025-11-20) Renamed `pcall_continuation` → `PcallContinuation` and `pcall_onerror` → `PcallOnError`, updated the delegate factories, and added null-argument guards so stack traces stay PascalCase and CA1062 is satisfied without suppressions.
1. **Lua port strategy**
   - ✅ (2025-11-20) Relocated `KopiLuaStringLib` to `src/runtime/NovaSharp.Interpreter/LuaPort`, added an explicit header comment describing the naming divergence, and disabled IDE1006 locally so the audit no longer needs bespoke member allowlists for this file.
   - ✅ (2025-11-20) Moved `CharPtr`, `LuaBase`, `LuaBaseCLib`, `LuaLBuffer`, `LuaState`, and `Tools` under `LuaPort/LuaStateInterop`, switched them to the `NovaSharp.Interpreter.LuaPort.LuaStateInterop` namespace, and added IDE1006 suppressions plus documentation so their snake_case members remain in sync with the Lua C API. Updated all call sites/tests and refreshed the naming-audit allowlists so the analyzer does not emit false positives.
   - For newly written code paths (e.g., `Utf8Module`), keep PascalCase even when mirroring Lua names and use the existing `NovaSharpModuleMethodAttribute(Name = "str_find")` to control the public Lua symbol instead of encoding the Lua name in the method identifier.
1. **Analyzer enforcement**
   - ✅ (2025-11-20) Added `.editorconfig` rules that force PascalCase for every method (`methods_must_be_pascal`) and `_camelCase` for private fields, excluding only the `NovaSharp.Interpreter.LuaPort` namespace so the mirrored Lua sources remain untouched. IDE1006 now fires as an error outside the LuaPort folder, immediately surfacing snake_case regressions.
   - ✅ (2025-11-20) CI now runs `dotnet csharpier check .` inside the lint job (via `scripts/ci/check-csharpier.sh`), so pull requests fail automatically when the canonical formatter detects drift. Any residual `dotnet format` complaints are treated as configuration bugs to fix rather than formatting to change.
1. **Audit automation**
   - ✅ (2025-11-22) Extended `tools/NamingAudit/naming_audit.py` with deterministic `--write-log`/`--verify-log` switches so the committed `docs/audits/naming_audit.log` always reflects the current analyzer output. The lint and dotnet-test workflows verify the log during every run, keeping the PLAN checkpoint honest without manual intervention.

## Next Steps

- Expand the audit report with per-identifier suppression metadata (why a member remains snake_case) so contributors know when a rename is safe vs. intentionally tied to Lua interop.
- Track Lua-port divergence explicitly (list all files under `LuaPort/` + justification) and document the process for updating the allowlist whenever we add or remove mirrored files.

## LuaPort Naming Allowlist

The only intentional snake_case identifiers that remain in the runtime live under `src/runtime/NovaSharp.Interpreter/LuaPort`. The table below documents the scope and rationale for keeping those names so reviewers can quickly decide whether a future rename is safe or breaks parity with upstream Lua sources.

| Path                                                                                                             | Reason for snake_case                                                                                                                                                                                                                                                                                                                                                                 | Notes                                                                                                                                                                                                             |
| ---------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `LuaPort/KopiLuaStringLib.cs`                                                                                    | Direct port of the Lua 5.4 string library from KopiLua. Keeping the original helper names (`match_class`, `gmatch_aux`, etc.) makes upstream diffs and manual audits trivial and avoids desynchronising the TAP fixtures we mirror from the Lua manual.                                                                                                                               | Wrapped in its own namespace plus IDE1006 suppression comment; new NovaSharp-owned string functions must continue to use PascalCase and expose Lua names via attributes.                                          |
| `LuaPort/LuaStateInterop/CharPtr.cs`, `LuaBase.cs`, `LuaBaseCLib.cs`, `LuaLBuffer.cs`, `LuaState.cs`, `Tools.cs` | These files mirror the Lua C API surface so host integrations (Unity, hardwire generators) can consume the same constants/method names when running mixed C#/Lua workloads. Leaving identifiers like `LuaMultipleResults`, `lua_pushvalue`, or `luaopen_base` untouched keeps the mental model aligned with the upstream manual and avoids churn when syncing bug fixes from KopiLua. | The entire `NovaSharp.Interpreter.LuaPort.LuaStateInterop` namespace is excluded from IDE1006 in `.editorconfig`. Any new helpers added outside this namespace must follow the normal PascalCase/CamelCase rules. |

If additional Lua-port files are introduced, document them here and explain why keeping the upstream casing is necessary. Otherwise, assume every new runtime/tooling/test identifier must follow PascalCase (methods/types) or `_camelCase` (private fields) and will fail CI via the enforced `.editorconfig` rules.
