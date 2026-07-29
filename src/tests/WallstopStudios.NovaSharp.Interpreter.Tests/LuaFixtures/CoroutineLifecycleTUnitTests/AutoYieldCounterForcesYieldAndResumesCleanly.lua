-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:127
-- @test: CoroutineLifecycleTUnitTests.AutoYieldCounterForcesYieldAndResumesCleanly
function heavy()
                    local sum = 0
                    for i = 1, 400 do
                        sum = sum + i
                    end
                    return sum
                end
