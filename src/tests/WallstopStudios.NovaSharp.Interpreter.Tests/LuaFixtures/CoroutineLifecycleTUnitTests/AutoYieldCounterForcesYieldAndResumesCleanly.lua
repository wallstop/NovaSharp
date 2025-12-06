-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/CoroutineLifecycleTUnitTests.cs:98
-- @test: CoroutineLifecycleTUnitTests.AutoYieldCounterForcesYieldAndResumesCleanly
-- @compat-notes: Lua 5.3+: bitwise operators
function heavy()
                    local sum = 0
                    for i = 1, 400 do
                        sum = sum + i
                    end
                    return sum
                end
