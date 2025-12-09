-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineApiTUnitTests.cs:408
-- @test: ProcessorCoroutineApiTUnitTests.AutoYieldCounterProxiesProcessorValue
return function() coroutine.yield(1) end
