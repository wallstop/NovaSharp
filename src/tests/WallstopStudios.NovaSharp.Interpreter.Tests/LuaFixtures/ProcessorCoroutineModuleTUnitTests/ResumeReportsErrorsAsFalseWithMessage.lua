-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoroutineModuleTUnitTests.cs:247
-- @test: ProcessorCoroutineModuleTUnitTests.ResumeReportsErrorsAsFalseWithMessage
function explode()
                    error('boom', 0)
                end
