-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/CoroutineLifecycleTUnitTests.cs:20
-- @test: CoroutineLifecycleTUnitTests.ResumeAfterCompletionThrowsCannotResumeNotSuspended
function simple() return 5 end
