-- @lua-versions: 5.1, 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:428
-- @test: ProcessorCoroutineCloseTUnitTests.CoroutineCloseIsNilInPreLua54
-- @compat-notes: Test targets Lua 5.1
return coroutine.close
