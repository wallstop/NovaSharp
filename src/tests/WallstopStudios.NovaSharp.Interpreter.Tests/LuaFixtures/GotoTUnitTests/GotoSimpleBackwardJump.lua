-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/GotoTUnitTests.cs:56
-- @test: GotoTUnitTests.GotoSimpleBackwardJump
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: goto statement (5.2+); Lua 5.2+: label (5.2+)
function test()
                    x = 5
                    ::jump::
                    if (x == 3) then return x end
                    x = 3
                    goto jump
                end
                return test()
