-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:509
-- @test: ProcessorCoroutineModuleTUnitTests.InitialResumeObjectArrayPreservesSpreadAndSingleObjectForms
-- Compatibility notes: Test targets Lua 5.1
return function(...) return select('#', ...), type((...)), ... end
