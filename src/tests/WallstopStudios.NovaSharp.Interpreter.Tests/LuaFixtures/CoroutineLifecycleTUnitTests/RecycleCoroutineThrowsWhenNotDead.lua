-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/CoroutineLifecycleIntegrationTUnitTests.cs:100
-- @test: CoroutineLifecycleTUnitTests.RecycleCoroutineThrowsWhenNotDead
-- @compat-notes: Test targets Lua 5.1
function sample() coroutine.yield(1) end
