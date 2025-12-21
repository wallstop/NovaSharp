-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:293
-- @test: ProcessorCoroutineCloseTUnitTests.CloseForceSuspendedCoroutineUnwindsSuccessfully
-- @compat-notes: Test targets Lua 5.1
function heavy_close()
                    local total = 0
                    for i = 1, 500 do
                        total = total + i
                    end
                    return total
                end
