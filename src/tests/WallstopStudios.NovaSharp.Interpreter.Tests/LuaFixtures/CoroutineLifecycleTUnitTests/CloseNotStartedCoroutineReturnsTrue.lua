-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/CoroutineLifecycleIntegrationTUnitTests.cs:336
-- @test: CoroutineLifecycleTUnitTests.CloseNotStartedCoroutineReturnsTrue
-- @compat-notes: Test targets Lua 5.1
function never_started() return 5 end
