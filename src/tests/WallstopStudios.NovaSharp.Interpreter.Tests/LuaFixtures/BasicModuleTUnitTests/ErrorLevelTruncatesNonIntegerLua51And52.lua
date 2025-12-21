-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:853
-- @test: BasicModuleTUnitTests.ErrorLevelTruncatesNonIntegerLua51And52
-- @compat-notes: Test targets Lua 5.1
error('test message', 1.5)
