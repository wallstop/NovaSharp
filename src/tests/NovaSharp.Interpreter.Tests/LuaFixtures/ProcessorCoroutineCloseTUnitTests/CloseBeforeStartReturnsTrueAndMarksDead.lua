-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineCloseTUnitTests.cs:18
-- @test: ProcessorCoroutineCloseTUnitTests.CloseBeforeStartReturnsTrueAndMarksDead
function ready() return 1 end
