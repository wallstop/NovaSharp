-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoroutineModuleTUnitTests.cs:140
-- @test: ProcessorCoroutineModuleTUnitTests.StatusThrowsForUnknownStates
function idle()
                    return 1
                end
