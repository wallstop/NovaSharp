-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/CoroutineLifecycleIntegrationTUnitTests.cs:280
-- @test: CoroutineLifecycleTUnitTests.CloseNotStartedCoroutineReturnsTrue
function never_started() return 5 end
