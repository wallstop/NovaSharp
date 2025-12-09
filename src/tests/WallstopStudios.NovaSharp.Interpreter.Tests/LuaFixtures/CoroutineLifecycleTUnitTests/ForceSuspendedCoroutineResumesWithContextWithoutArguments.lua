-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/CoroutineLifecycleIntegrationTUnitTests.cs:161
-- @test: CoroutineLifecycleTUnitTests.ForceSuspendedCoroutineResumesWithContextWithoutArguments
-- @compat-notes: Lua 5.3+: bitwise operators
function heavyweight()
                    local total = 0
                    for i = 1, 300 do
                        total = total + i
                    end
                    return total
                end
