-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ForLoopConstVariableTUnitTests.cs:22
-- @test: ForLoopConstVariableTUnitTests.NumericForControlVariableRejectsAssignment
-- Compatibility notes: Test targets Lua 5.5+
for i = 1, 3 do i = i + 1 end
