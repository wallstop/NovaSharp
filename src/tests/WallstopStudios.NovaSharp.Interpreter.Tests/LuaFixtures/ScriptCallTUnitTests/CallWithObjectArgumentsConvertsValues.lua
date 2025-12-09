-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCallTUnitTests.cs:102
-- @test: ScriptCallTUnitTests.CallWithObjectArgumentsConvertsValues
function add(a, b) return a + b end
