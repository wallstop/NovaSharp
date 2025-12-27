-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/GotoTUnitTests.cs:87
-- @test: GotoTUnitTests.GotoDoubleDefinedLabelThrows
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: label (5.2+)
::label::
                ::label::
