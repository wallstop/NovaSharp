-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\CoroutineLifecycleIntegrationTUnitTests.cs:98
-- @test: CoroutineLifecycleTUnitTests.AutoYieldCounterForcesYieldAndResumesCleanly
-- @compat-notes: Lua 5.3+: bitwise operators
function heavy()
                    local sum = 0
                    for i = 1, 400 do
                        sum = sum + i
                    end
                    return sum
                end
