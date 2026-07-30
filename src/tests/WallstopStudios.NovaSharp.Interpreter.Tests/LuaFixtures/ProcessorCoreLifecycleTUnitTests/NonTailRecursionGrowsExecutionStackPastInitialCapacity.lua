-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoreLifecycleTUnitTests.cs:303
-- @test: ProcessorCoreLifecycleTUnitTests.NonTailRecursionGrowsExecutionStackPastInitialCapacity
local function recurse(n)
                    if n == 0 then
                        return 1
                    end

                    return 1 + recurse(n - 1)
                end

                return recurse(80)
