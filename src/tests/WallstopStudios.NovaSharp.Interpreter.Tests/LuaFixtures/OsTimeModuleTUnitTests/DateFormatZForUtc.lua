-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:617
-- @test: OsTimeModuleTUnitTests.DateFormatZForUtc
-- @compat-notes: Test targets Lua 5.1
return os.date('!%z', 0)
