-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:228
-- @test: ProcessorCoroutineCloseTUnitTests.CloseUnknownStateThrows
-- @compat-notes: Test targets Lua 5.1
function idle() coroutine.yield('pause') end
