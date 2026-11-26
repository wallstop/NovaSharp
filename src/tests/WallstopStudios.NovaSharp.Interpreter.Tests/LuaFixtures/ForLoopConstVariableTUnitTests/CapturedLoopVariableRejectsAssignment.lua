-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ForLoopConstVariableTUnitTests.cs:52
-- @test: ForLoopConstVariableTUnitTests.CapturedLoopVariableRejectsAssignment
-- Compatibility notes: Test targets Lua 5.5+
local function update() for i = 1, 3 do return function() i = 2 end end end
