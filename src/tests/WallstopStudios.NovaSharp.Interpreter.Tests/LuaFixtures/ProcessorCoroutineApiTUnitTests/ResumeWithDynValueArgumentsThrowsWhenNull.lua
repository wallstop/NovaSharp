-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineApiTUnitTests.cs:274
-- @test: ProcessorCoroutineApiTUnitTests.ResumeWithDynValueArgumentsThrowsWhenNull
return function() return 1 end
