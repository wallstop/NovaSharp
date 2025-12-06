-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineApiTUnitTests.cs:76
-- @test: ProcessorCoroutineApiTUnitTests.AsEnumerableReturnsObjects
return function() coroutine.yield(10) coroutine.yield(20) return 30 end
