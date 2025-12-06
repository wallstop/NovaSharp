-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineApiTUnitTests.cs:343
-- @test: ProcessorCoroutineApiTUnitTests.AutoYieldCounterForcesSuspendUntilResumed
return function() return 42 end
