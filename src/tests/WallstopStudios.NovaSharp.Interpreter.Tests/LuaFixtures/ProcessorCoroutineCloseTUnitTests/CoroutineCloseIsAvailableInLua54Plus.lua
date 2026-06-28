-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoroutineCloseTUnitTests.cs:445
-- @test: ProcessorCoroutineCloseTUnitTests.CoroutineCloseIsAvailableInLua54Plus
-- Test targets Lua 5.3+
return coroutine.close
