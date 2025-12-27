-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/GotoTUnitTests.cs:159
-- @test: GotoTUnitTests.GotoCannotJumpOverLocalDeclarations
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: goto statement (5.2+); Lua 5.2+: label (5.2+)
-- Per Lua 5.4 §3.5, adding print(x) ensures the label is NOT at the end of block
goto f
local x
::f::
print(x)