-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCallTUnitTests.cs:137
-- @test: ScriptCallTUnitTests.CallWithObjectArgumentsConvertsValues
-- @compat-notes: Test targets Lua 5.1
function add(a, b) return a + b end
