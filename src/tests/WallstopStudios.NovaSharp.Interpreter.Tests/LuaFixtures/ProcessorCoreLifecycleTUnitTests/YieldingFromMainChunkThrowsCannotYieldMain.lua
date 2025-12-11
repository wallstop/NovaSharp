-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoreLifecycleTUnitTests.cs:225
-- @test: ProcessorCoreLifecycleTUnitTests.YieldingFromMainChunkThrowsCannotYieldMain
coroutine.yield('outside')
