-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:583
-- @test: NumericForLoopTUnitTests.NonNumberControlErrorsUseLua54MessageFormat
-- Compatibility notes: Test targets Lua 5.4+
for i = 1, {}, 1 do end
