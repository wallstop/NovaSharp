-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineApiTUnitTests.cs:90
-- @test: ProcessorCoroutineApiTUnitTests.AsEnumerableOfTReturnsTypedScalars
return function() coroutine.yield(1) coroutine.yield(2) return 3 end
