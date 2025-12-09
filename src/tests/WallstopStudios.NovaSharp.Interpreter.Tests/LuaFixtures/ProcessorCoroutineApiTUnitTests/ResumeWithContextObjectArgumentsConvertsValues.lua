-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoroutineApiTUnitTests.cs:247
-- @test: ProcessorCoroutineApiTUnitTests.ResumeWithContextObjectArgumentsConvertsValues
return function(a, b) return a + b end
