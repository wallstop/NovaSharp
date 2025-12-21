-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/CoroutineLifecycleIntegrationTUnitTests.cs:27
-- @test: CoroutineLifecycleTUnitTests.ResumeAfterCompletionThrowsCannotResumeNotSuspended
-- @compat-notes: Test targets Lua 5.1
function simple() return 5 end
