-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptExecutionContextTUnitTests.cs:2169
-- @test: ScriptExecutionContextTUnitTests.IsYieldableReturnsTrueInsideCoroutine
-- Compatibility notes: Test targets Lua 5.4+
function coroutineProbe() return yieldState() end
