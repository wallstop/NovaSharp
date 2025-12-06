-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineLifecycleTUnitTests.cs:79
-- @test: ProcessorCoroutineLifecycleTUnitTests.YieldingFromMainChunkThrowsCannotYieldMain
coroutine.yield('outside')
