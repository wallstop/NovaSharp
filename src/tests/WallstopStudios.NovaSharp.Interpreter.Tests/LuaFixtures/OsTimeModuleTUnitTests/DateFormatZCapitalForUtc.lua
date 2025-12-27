-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:632
-- @test: OsTimeModuleTUnitTests.DateFormatZCapitalForUtc
-- @compat-notes: Test targets Lua 5.1
return os.date('!%Z', 0)
