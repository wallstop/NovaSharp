-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:367
-- @test: NumericForLoopTUnitTests.ControlVariableIsOutOfScopeAfterLoop
-- Compatibility notes: Test targets Lua 5.4+
for i = -2, 2 do end return i
