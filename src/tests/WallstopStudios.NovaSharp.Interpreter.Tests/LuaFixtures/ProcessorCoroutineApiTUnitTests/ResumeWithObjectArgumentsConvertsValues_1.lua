-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineApiTUnitTests.cs:326
-- @test: ProcessorCoroutineApiTUnitTests.ResumeWithObjectArgumentsConvertsValues
return function(value) return value end
