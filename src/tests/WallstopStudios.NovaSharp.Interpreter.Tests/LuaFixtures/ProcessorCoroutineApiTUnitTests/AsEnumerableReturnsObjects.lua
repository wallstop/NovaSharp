-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineApiTUnitTests.cs:77
-- @test: ProcessorCoroutineApiTUnitTests.AsEnumerableReturnsObjects
return function() coroutine.yield(10) coroutine.yield(20) return 30 end
