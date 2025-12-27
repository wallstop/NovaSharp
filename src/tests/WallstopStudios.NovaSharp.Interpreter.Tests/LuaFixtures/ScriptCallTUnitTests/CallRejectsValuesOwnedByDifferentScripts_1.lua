-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCallTUnitTests.cs:418
-- @test: ScriptCallTUnitTests.CallRejectsValuesOwnedByDifferentScripts
-- @compat-notes: Test targets Lua 5.1
function echo(value) return value end
