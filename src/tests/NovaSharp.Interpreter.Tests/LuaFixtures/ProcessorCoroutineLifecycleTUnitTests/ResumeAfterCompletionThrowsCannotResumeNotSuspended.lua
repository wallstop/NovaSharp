-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineLifecycleTUnitTests.cs:22
-- @test: ProcessorCoroutineLifecycleTUnitTests.ResumeAfterCompletionThrowsCannotResumeNotSuspended
function simple() return 5 end
