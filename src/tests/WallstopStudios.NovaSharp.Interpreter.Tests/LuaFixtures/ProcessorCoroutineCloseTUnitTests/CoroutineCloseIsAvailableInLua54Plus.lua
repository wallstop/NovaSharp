-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:445
-- @test: ProcessorCoroutineCloseTUnitTests.CoroutineCloseIsAvailableInLua54Plus
-- @compat-notes: Test targets Lua 5.3+
return coroutine.close
