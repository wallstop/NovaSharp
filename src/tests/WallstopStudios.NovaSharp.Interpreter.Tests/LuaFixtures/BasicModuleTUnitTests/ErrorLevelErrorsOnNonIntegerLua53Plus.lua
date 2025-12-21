-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:839
-- @test: BasicModuleTUnitTests.ErrorLevelErrorsOnNonIntegerLua53Plus
-- @compat-notes: Test targets Lua 5.1
error('test', 1.5)
