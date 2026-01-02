-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\GotoTUnitTests.cs:68
-- @test: GotoTUnitTests.GotoUndefinedLabelThrows
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: goto statement (5.2+)
goto there
