-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/CoroutineLifecycleIntegrationTUnitTests.cs:83
-- @test: CoroutineLifecycleTUnitTests.RecycleCoroutineThrowsWhenNotDead
function sample() coroutine.yield(1) end
