-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:2469
-- @test: ScriptCallTUnitTests.CallWithReadOnlySpanDynValuesPreservesAdjustmentSemantics
return function(...)
                    local count = select('#', ...)
                    local nils = 0
                    local sum = 0
                    for i = 1, count do
                        local value = select(i, ...)
                        if value == nil then
                            nils = nils + 1
                        else
                            sum = sum + value
                        end
                    end

                    return count, nils, sum
                end
