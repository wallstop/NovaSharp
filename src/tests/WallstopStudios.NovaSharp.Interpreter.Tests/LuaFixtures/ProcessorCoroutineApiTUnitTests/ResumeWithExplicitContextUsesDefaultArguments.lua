-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoroutineApiTUnitTests.cs:221
-- @test: ProcessorCoroutineApiTUnitTests.ResumeWithExplicitContextUsesDefaultArguments
return function() return 5 end
