-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ForLoopConstVariableTUnitTests.cs:37
-- @test: ForLoopConstVariableTUnitTests.GenericForControlVariableRejectsAssignment
-- Compatibility notes: Test targets Lua 5.5+
for key, value in pairs({ 1 }) do key = key + 1 end
