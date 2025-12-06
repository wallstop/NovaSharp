-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineApiTUnitTests.cs:137
-- @test: ProcessorCoroutineApiTUnitTests.MarkClrCallbackAsDeadThrowsWhenCoroutineNotCallback
return function() return 1 end
