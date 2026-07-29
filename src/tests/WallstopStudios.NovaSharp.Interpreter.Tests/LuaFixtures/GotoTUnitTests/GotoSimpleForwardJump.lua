-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\GotoTUnitTests.cs:35
-- @test: GotoTUnitTests.GotoSimpleForwardJump
-- Test targets Lua 5.2+; Lua 5.2+: goto statement (5.2+); Lua 5.2+: label (5.2+)
function test()
                    x = 3
                    goto skip
                    x = x + 2
                    ::skip::
                    return x
                end
                return test()
