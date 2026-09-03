-- @lua-versions: 5.1-5.3
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:562
-- @test: NumericForLoopTUnitTests.InvalidLimitReportedBeforeZeroStepThroughLua53
-- Compatibility notes: Test targets Lua 5.3+
for i = 1, {}, 0 do end
