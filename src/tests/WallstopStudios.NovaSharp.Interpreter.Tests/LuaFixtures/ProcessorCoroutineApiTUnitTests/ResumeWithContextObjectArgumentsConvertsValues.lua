-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineApiTUnitTests.cs:236
-- @test: ProcessorCoroutineApiTUnitTests.ResumeWithContextObjectArgumentsConvertsValues
return function(a, b) return a + b end
