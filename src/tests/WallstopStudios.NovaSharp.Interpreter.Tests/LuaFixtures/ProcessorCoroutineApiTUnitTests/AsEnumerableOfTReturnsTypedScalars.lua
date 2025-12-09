-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoroutineApiTUnitTests.cs:101
-- @test: ProcessorCoroutineApiTUnitTests.AsEnumerableOfTReturnsTypedScalars
return function() coroutine.yield(1) coroutine.yield(2) return 3 end
