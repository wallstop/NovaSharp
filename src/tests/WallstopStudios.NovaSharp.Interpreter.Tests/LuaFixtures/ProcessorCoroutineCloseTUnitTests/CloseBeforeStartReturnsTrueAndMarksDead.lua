-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ProcessorExecution\ProcessorCoroutineCloseTUnitTests.cs:18
-- @test: ProcessorCoroutineCloseTUnitTests.CloseBeforeStartReturnsTrueAndMarksDead
function ready() return 1 end
