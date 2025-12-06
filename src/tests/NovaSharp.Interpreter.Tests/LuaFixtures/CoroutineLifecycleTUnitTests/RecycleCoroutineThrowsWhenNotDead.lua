-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/CoroutineLifecycleTUnitTests.cs:83
-- @test: CoroutineLifecycleTUnitTests.RecycleCoroutineThrowsWhenNotDead
function sample() coroutine.yield(1) end
