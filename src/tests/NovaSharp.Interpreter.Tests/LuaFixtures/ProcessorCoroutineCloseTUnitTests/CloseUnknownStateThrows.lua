-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineCloseTUnitTests.cs:183
-- @test: ProcessorCoroutineCloseTUnitTests.CloseUnknownStateThrows
function idle() coroutine.yield('pause') end
