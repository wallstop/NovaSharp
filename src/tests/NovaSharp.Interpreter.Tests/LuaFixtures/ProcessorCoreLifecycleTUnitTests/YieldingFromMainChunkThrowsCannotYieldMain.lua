-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoreLifecycleTUnitTests.cs:222
-- @test: ProcessorCoreLifecycleTUnitTests.YieldingFromMainChunkThrowsCannotYieldMain
coroutine.yield('outside')
