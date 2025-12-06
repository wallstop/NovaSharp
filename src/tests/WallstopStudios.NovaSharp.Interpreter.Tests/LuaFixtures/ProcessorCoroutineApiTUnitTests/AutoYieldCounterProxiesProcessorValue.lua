-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineApiTUnitTests.cs:397
-- @test: ProcessorCoroutineApiTUnitTests.AutoYieldCounterProxiesProcessorValue
return function() coroutine.yield(1) end
