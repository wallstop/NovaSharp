-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:539
-- @test: ProcessorCoroutineModuleTUnitTests.InitialResumeObjectArgumentsSupportsCallerOwnedSpanSlice
return function(...) return select('#', ...), ... end
