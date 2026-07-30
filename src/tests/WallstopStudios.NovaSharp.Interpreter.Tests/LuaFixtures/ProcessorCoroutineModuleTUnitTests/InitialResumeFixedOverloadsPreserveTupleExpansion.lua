-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:225
-- @test: ProcessorCoroutineModuleTUnitTests.InitialResumeFixedOverloadsPreserveTupleExpansion
-- Compatibility notes: Test targets Lua 5.1
return function(...) return select('#', ...), ... end
