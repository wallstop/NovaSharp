-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ScriptCallTUnitTests.cs:305
-- @test: ScriptCallTUnitTests.CallRejectsValuesOwnedByDifferentScripts
function echo(value) return value end
