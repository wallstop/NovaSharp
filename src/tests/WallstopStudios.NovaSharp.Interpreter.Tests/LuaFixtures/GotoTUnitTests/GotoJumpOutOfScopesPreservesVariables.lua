-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/GotoTUnitTests.cs:248
-- @test: GotoTUnitTests.GotoJumpOutOfScopesPreservesVariables
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: goto statement (5.2+); Lua 5.2+: label (5.2+)
local u = 4
                do
                    local x = 5
                    do
                        local y = 6
                        do
                            goto out
                            local z = 7
                        end
                    end
                end
                ::out::
                do
                    local a
                    local b = 55
                    if (a == nil) then
                        b = b + 12
                    end
                    return b
                end
