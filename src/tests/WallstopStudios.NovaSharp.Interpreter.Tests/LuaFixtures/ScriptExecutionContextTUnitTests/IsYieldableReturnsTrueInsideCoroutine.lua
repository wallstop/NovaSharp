-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptExecutionContextTUnitTests.cs:418
-- @test: ScriptExecutionContextTUnitTests.IsYieldableReturnsTrueInsideCoroutine
function coroutineProbe() return yieldState() end
