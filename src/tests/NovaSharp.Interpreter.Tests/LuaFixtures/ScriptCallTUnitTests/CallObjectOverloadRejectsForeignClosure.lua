-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ScriptCallTUnitTests.cs:317
-- @test: ScriptCallTUnitTests.CallObjectOverloadRejectsForeignClosure
function noop() return 1 end
