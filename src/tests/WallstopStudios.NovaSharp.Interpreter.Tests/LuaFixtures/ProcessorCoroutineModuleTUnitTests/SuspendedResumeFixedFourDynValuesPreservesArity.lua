-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:302
-- @test: ProcessorCoroutineModuleTUnitTests.SuspendedResumeFixedFourDynValuesPreservesArity
-- Compatibility notes: Test targets Lua 5.1
return function() local a, b, c, d = coroutine.yield('ready') return select('#', a, b, c, d), a, b, c, d end
