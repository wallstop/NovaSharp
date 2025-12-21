-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineApiTUnitTests.cs:209
-- @test: ProcessorCoroutineApiTUnitTests.StateTransitionsFollowCoroutineLifecycle
-- @compat-notes: Test targets Lua 5.1
return function() coroutine.yield(1) coroutine.yield(2) end
