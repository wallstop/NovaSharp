-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsExecuteVersionParityTUnitTests.cs:40
-- @test: OsExecuteVersionParityTUnitTests.OsExecuteReturnsNumberInLua51
-- @compat-notes: Test targets Lua 5.1
return os.execute('build')
