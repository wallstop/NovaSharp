-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:200
-- @test: ProcessorCoroutineCloseTUnitTests.CloseDeadCoroutineWithoutErrorsReturnsTrue
function done() return 'ok' end
