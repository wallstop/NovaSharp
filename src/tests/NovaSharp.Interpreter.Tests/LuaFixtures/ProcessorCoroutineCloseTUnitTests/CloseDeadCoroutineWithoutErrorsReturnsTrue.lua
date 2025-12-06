-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineCloseTUnitTests.cs:200
-- @test: ProcessorCoroutineCloseTUnitTests.CloseDeadCoroutineWithoutErrorsReturnsTrue
function done() return 'ok' end
