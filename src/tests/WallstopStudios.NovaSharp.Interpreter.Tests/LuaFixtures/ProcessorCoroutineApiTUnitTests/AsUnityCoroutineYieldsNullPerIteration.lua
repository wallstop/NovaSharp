-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineApiTUnitTests.cs:114
-- @test: ProcessorCoroutineApiTUnitTests.AsUnityCoroutineYieldsNullPerIteration
return function() coroutine.yield('a') coroutine.yield('b') return 'c' end
