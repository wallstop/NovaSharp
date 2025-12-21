-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/GotoTUnitTests.cs:186
-- @test: GotoTUnitTests.GotoJumpOutOfBlocksReturnsValue
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: goto statement (5.2+); Lua 5.2+: label (5.2+)
local u = 4
                do
                    local x = 5
                    do
                        local y = 6
                        do
                            local z = 7
                        end
                        goto out
                    end
                end
                do return 5 end
                ::out::
                return 3
