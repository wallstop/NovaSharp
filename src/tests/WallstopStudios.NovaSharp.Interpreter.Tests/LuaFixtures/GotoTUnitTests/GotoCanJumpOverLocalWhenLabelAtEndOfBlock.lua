-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/GotoTUnitTests.cs
-- @test: GotoTUnitTests.GotoCanJumpOverLocalWhenLabelAtEndOfBlock
-- @compat-notes: Test targets Lua 5.2+; Per Lua 5.4 §3.5, void statement rule allows this
-- Per Lua 5.4 §3.5: when label is at end of block, locals declared before are not in scope
goto f
local x = 1
::f::