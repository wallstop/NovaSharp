-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/GotoTUnitTests.cs:68
-- @test: GotoTUnitTests.GotoUndefinedLabelThrows
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: goto statement (5.2+)
goto there
