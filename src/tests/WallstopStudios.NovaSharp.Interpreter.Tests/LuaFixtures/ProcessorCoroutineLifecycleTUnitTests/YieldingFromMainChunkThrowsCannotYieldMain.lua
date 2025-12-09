-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineLifecycleTUnitTests.cs:79
-- @test: ProcessorCoroutineLifecycleTUnitTests.YieldingFromMainChunkThrowsCannotYieldMain
coroutine.yield('outside')
