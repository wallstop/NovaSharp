-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCallTUnitTests.cs:396
-- @test: ScriptCallTUnitTests.CreateCoroutineObjectOverloadRejectsForeignClosure
function noop() return 0 end
