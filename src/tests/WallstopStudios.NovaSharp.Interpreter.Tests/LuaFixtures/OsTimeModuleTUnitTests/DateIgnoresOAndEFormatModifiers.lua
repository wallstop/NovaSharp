-- @lua-versions: 5.1, 5.2, 5.3, 5.4
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:294
-- @test: OsTimeModuleTUnitTests.DateIgnoresOAndEFormatModifiers
-- @compat-notes: NovaSharp divergence: Reference Lua rejects %O and %E modifiers as invalid
--   conversion specifiers across all versions, but NovaSharp intentionally ignores them
--   for compatibility with code that may use these POSIX-style modifiers
return os.date('!%OY-%Ew', 0)
