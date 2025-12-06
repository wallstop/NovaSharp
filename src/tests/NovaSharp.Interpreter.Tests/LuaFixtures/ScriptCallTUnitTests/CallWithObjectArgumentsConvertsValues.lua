-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ScriptCallTUnitTests.cs:102
-- @test: ScriptCallTUnitTests.CallWithObjectArgumentsConvertsValues
function add(a, b) return a + b end
