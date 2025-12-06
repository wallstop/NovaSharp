-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/CoroutineLifecycleTUnitTests.cs:280
-- @test: CoroutineLifecycleTUnitTests.CloseNotStartedCoroutineReturnsTrue
function never_started() return 5 end
