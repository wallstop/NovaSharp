-- @lua-versions: 5.1, 5.2, 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:294
-- @test: OsTimeModuleTUnitTests.DateIgnoresOAndEFormatModifiers
-- @compat-notes: Lua 5.5 made %O and %E invalid conversion specifiers; pre-5.5 ignores them
return os.date('!%OY-%Ew', 0)
