-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ScriptExecutionContextTUnitTests.cs:405
-- @test: ScriptExecutionContextTUnitTests.IsYieldableReturnsTrueInsideCoroutine
function coroutineProbe() return yieldState() end
