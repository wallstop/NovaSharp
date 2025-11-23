# Lua Compatibility Profiles

NovaSharp targets Lua 5.4.8 today, but mod authors often need to pin older semantics when porting legacy scripts (Lua 5.2/5.3) or experimenting with preview language features (Lua 5.5). The new compatibility profile layer summarizes which behaviours ship with each `LuaCompatibilityVersion` value so the runtime, tooling, and documentation share a single vocabulary.

## Configuration Surface

| Entry Point         | API                                         | Notes                                                                                             |
| ------------------- | ------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| Global default      | `Script.GlobalOptions.CompatibilityVersion` | Sets the default profile for new `Script` instances.                                              |
| Per-script override | `script.Options.CompatibilityVersion`       | Allows individual scripts hosted in the same process to target different Lua baselines.           |
| Runtime view        | `script.CompatibilityProfile`               | Returns a read-only `LuaCompatibilityProfile` describing the active feature flags for the script. |

The `LuaCompatibilityProfile` exposes boolean feature toggles that map directly to spec additions between Lua versions. Downstream work (parser tweaks, library shims, analyzer hints) switches on these flags rather than hard-coding version checks.

### Runtime Metadata

Every globals table now exposes `_G._NovaSharp.luacompat`, which holds the active compatibility profile display name (for example, "Lua 5.3"). Hosts and modding tools can read this value to confirm which feature set a script is currently running under without reaching back into the hosting API.

### Tooling Surfacing

- The NovaSharp CLI banner prints `[compatibility] Active profile: …` before the REPL prompt so every session immediately documents the active Lua baseline and how to change it (`Script.Options.CompatibilityVersion` or `luaCompatibility` in `mod.json`).
- Launching a script via `NovaSharp <file.lua>` or the `!run <file>` command now logs `[compatibility] Running '<file>' with …` regardless of whether a manifest applied custom options. This keeps one-off script executions and manifest-driven runs equally transparent.
- The NovaSharp CLI `!help` output now prints `Active compatibility profile: …` along with the key feature toggles (bitwise operators, `bit32`, `utf8`, `table.move`, `<const>`, `<close>`, `warn`). This gives script authors an immediate reminder of which Lua baseline the REPL is running under and which spec features are currently enabled without digging through options.
- The CLI debugger entry point (`!debug`) emits a `[compatibility] Debugger session running under …` line before attaching the remote debugger bridge so logs capture the Lua profile that governed the session. This mirrors the `[compatibility]` warnings produced by manifest processing and keeps debugger transcripts self-describing once bitwise/floor-division gating is toggled per profile.
- `RemoteDebuggerService.AttachFromDirectory` now pushes a summary message through its `infoSink` so hosts (tutorials, Unity samples, CLI helpers) automatically log the compatibility profile applied to manifest-driven mods.
- The VS Code debug adapter surfaces the same message inside the debug console during `Initialize`, giving attach sessions an immediate reminder of the active Lua version/feature toggles.

## Feature Matrix

| Feature                    | Lua 5.2        | Lua 5.3 | Lua 5.4 | Lua 5.5 / Latest | Reference                                                                                               |
| -------------------------- | -------------- | ------- | ------- | ---------------- | ------------------------------------------------------------------------------------------------------- |
| `bit32` library            | ✅             | ❌      | ❌      | ❌               | Lua 5.2 reference manual §6.7 (NovaSharp only exposes the compatibility library when targeting Lua 5.2) |
| Bitwise operators (`&`, \` | `, `~\`, etc.) | ❌      | ✅      | ✅               | ✅                                                                                                      |
| `utf8` standard library    | ❌             | ✅      | ✅      | ✅               | Lua 5.3 manual §6.5                                                                                     |
| `table.move` helper        | ❌             | ✅      | ✅      | ✅               | Lua 5.3 manual §6.5                                                                                     |
| `<close>` variables        | ❌             | ❌      | ✅      | ✅               | Lua 5.4 manual §3.3.8                                                                                   |
| `<const>` locals           | ❌             | ❌      | ✅      | ✅               | Lua 5.4 manual §3.3.7                                                                                   |
| Built-in `warn` function   | ❌             | ❌      | ✅      | ✅               | Lua 5.4 manual §6.1                                                                                     |

> ℹ️ Future additions (e.g., Lua 5.5 once the upstream manual stabilizes) simply extend the profile table without touching every call site that reads these booleans.

## Next Steps

1. ✅ **Parser/AST toggles** – `AssignmentStatement` now checks the active `LuaCompatibilityProfile` before accepting `<const>`/`<close>` attributes, throwing spec-cited syntax errors (Lua 5.4 manual §§3.3.7–3.3.8) when a Lua 5.2/5.3 profile attempts to use them. Guarded by `AssignmentStatementTests.ConstAttributeRequiresLua54Compatibility` / `CloseAttributeRequiresLua54Compatibility`.

1. ✅ **Standard library shims** – `SupportsUtf8Library` now gates the `utf8` module (Lua 5.4 manual §6.5), `SupportsTableMove` strips `table.move` when profiles opt out (Lua 5.3 manual §6.6), `SupportsWarnFunction` removes the Lua 5.4+ `warn` helper when disabled (Lua 5.4 manual §6.1), and `SupportsBit32Library` hides the legacy `bit32` namespace unless the script targets Lua 5.2. Covered by `Utf8ModuleTests`, `CompatibilityVersionTests.TableMoveOnlyAvailableInLua53Plus`, `CompatibilityVersionTests.WarnFunctionOnlyAvailableInLua54Plus`, and `CompatibilityVersionTests.Bit32LibraryOnlyAvailableInLua52`.

1. ✅ **Documentation/diagnostics** – `InterpreterException` now appends `[compatibility: Lua X.Y]` to every decorated message so Syntax/Runtime errors cite the active profile. Guarded by `CompatibilityDiagnosticsTests`.

1. ✅ **Manifest plumbing** – mod manifests now accept a `luaCompatibility` field (e.g., `"Lua54"`, `"5.4"`, `"latest"`). Hosts can parse the JSON via `ModManifest.Parse` / `ModManifest.Load` and call `manifest.ApplyCompatibility(script.Options, hostCompatibility, warning => …)` to set `ScriptOptions.CompatibilityVersion` and log if the requested version exceeds the host’s baseline. Example:

   ```json
   {
     "name": "Example Mod",
     "version": "1.2.3",
     "luaCompatibility": "Lua53"
   }
   ```

   ```csharp
   ModManifest manifest = ModManifest.Parse(json);
   manifest.ApplyCompatibility(script.Options, Script.GlobalOptions.CompatibilityVersion, Console.WriteLine);
   ```

Tracking progress against each flag keeps PLAN.md actionable and gives contributors a concrete checklist whenever Lua publishes a new minor release.\*\*\*
