# Lua Compatibility Profiles

NovaSharp targets Lua 5.4.8 today, but mod authors often need to pin older semantics when porting legacy scripts (Lua 5.2/5.3) or experimenting with preview language features (Lua 5.5). The new compatibility profile layer summarizes which behaviours ship with each `LuaCompatibilityVersion` value so the runtime, tooling, and documentation share a single vocabulary.

## Configuration Surface

| Entry Point         | API                                         | Notes                                                                                             |
| ------------------- | ------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| Global default      | `Script.GlobalOptions.CompatibilityVersion` | Sets the default profile for new `Script` instances.                                              |
| Per-script override | `script.Options.CompatibilityVersion`       | Allows individual scripts hosted in the same process to target different Lua baselines.           |
| Runtime view        | `script.CompatibilityProfile`               | Returns a read-only `LuaCompatibilityProfile` describing the active feature flags for the script. |

The `LuaCompatibilityProfile` exposes boolean feature toggles that map directly to spec additions between Lua versions. Downstream work (parser tweaks, library shims, analyzer hints) switches on these flags rather than hard-coding version checks.

## Feature Matrix

| Feature                    | Lua 5.2        | Lua 5.3 | Lua 5.4 | Lua 5.5 / Latest | Reference                                                                                                     |
| -------------------------- | -------------- | ------- | ------- | ---------------- | ------------------------------------------------------------------------------------------------------------- |
| `bit32` library            | ✅             | ✅      | ✅      | ✅               | Lua 5.2 reference manual §6.7 (NovaSharp keeps the compatibility library enabled by default for all versions) |
| Bitwise operators (`&`, \` | `, `~\`, etc.) | ❌      | ✅      | ✅               | ✅                                                                                                            |
| `utf8` standard library    | ❌             | ✅      | ✅      | ✅               | Lua 5.3 manual §6.5                                                                                           |
| `table.move` helper        | ❌             | ✅      | ✅      | ✅               | Lua 5.3 manual §6.5                                                                                           |
| `<close>` variables        | ❌             | ❌      | ✅      | ✅               | Lua 5.4 manual §3.3.8                                                                                         |
| `<const>` locals           | ❌             | ❌      | ✅      | ✅               | Lua 5.4 manual §3.3.7                                                                                         |
| Built-in `warn` function   | ❌             | ❌      | ✅      | ✅               | Lua 5.4 manual §6.1                                                                                           |

> ℹ️ Future additions (e.g., Lua 5.5 once the upstream manual stabilizes) simply extend the profile table without touching every call site that reads these booleans.

## Next Steps

1. ✅ **Parser/AST toggles** – `AssignmentStatement` now checks the active `LuaCompatibilityProfile` before accepting `<const>`/`<close>` attributes, throwing spec-cited syntax errors (Lua 5.4 manual §§3.3.7–3.3.8) when a Lua 5.2/5.3 profile attempts to use them. Guarded by `AssignmentStatementTests.ConstAttributeRequiresLua54Compatibility` / `CloseAttributeRequiresLua54Compatibility`.
1. 🔄 **Standard library shims** – `SupportsUtf8Library` now gates the `utf8` module (Lua 5.4 manual §6.5) and the `Utf8ModuleTests` suite locks the charpattern/error semantics; `table.move` and `warn` still need the same treatment.
1. ✅ **Documentation/diagnostics** – `InterpreterException` now appends `[compatibility: Lua X.Y]` to every decorated message so Syntax/Runtime errors cite the active profile. Guarded by `CompatibilityDiagnosticsTests`.
1. **Manifest plumbing** – extend mod manifests with a `luaCompatibility` entry wired to `ScriptOptions.CompatibilityVersion` so hosts can warn ahead of time when a mod targets newer semantics than the runtime exposes.

Tracking progress against each flag keeps PLAN.md actionable and gives contributors a concrete checklist whenever Lua publishes a new minor release.\*\*\*
