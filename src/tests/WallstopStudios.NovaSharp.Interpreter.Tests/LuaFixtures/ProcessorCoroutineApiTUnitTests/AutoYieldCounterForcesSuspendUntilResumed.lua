-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineApiTUnitTests.cs:354
-- @test: ProcessorCoroutineApiTUnitTests.AutoYieldCounterForcesSuspendUntilResumed
return function() return 42 end
