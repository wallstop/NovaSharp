-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\CoroutineLifecycleIntegrationTUnitTests.cs:20
-- @test: CoroutineLifecycleTUnitTests.ResumeAfterCompletionThrowsCannotResumeNotSuspended
function simple() return 5 end
