-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ScriptCallTUnitTests.cs:396
-- @test: ScriptCallTUnitTests.CreateCoroutineObjectOverloadRejectsForeignClosure
function noop() return 0 end
