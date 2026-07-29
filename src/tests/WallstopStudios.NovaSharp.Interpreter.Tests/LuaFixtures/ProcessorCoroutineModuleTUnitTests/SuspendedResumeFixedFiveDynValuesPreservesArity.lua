-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:331
-- @test: ProcessorCoroutineModuleTUnitTests.SuspendedResumeFixedFiveDynValuesPreservesArity
-- Compatibility notes: Test targets Lua 5.1
return function() local a, b, c, d, e = coroutine.yield('ready') return select('#', a, b, c, d, e), a, b, c, d, e end
