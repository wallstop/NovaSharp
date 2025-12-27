-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:294
-- @test: OsTimeModuleTUnitTests.DateIgnoresOAndEFormatModifiers
-- @compat-notes: Test targets Lua 5.1
return os.date('!%OY-%Ew', 0)
