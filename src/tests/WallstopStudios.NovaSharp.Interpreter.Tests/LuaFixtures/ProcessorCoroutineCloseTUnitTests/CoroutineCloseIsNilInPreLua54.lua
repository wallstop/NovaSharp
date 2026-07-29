-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoroutineCloseTUnitTests.cs:428
-- @test: ProcessorCoroutineCloseTUnitTests.CoroutineCloseIsNilInPreLua54
-- Test targets Lua 5.1
return coroutine.close
