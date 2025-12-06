-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineApiTUnitTests.cs:103
-- @test: ProcessorCoroutineApiTUnitTests.AsUnityCoroutineYieldsNullPerIteration
return function() coroutine.yield('a') coroutine.yield('b') return 'c' end
