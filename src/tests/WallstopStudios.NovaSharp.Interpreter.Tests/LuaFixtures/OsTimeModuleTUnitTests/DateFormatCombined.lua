-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:647
-- @test: OsTimeModuleTUnitTests.DateFormatCombined
-- @compat-notes: Test targets Lua 5.1
return os.date('!%Y-%m-%d %H:%M:%S', 0)
