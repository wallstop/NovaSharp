-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:595
-- @test: ProcessorCoroutineModuleTUnitTests.InitialResumeObjectArgumentsWithNullArrayThrows
return function(...) return ... end
