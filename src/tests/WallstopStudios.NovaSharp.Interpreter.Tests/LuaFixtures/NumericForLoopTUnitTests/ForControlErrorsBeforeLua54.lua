-- @lua-versions: 5.1-5.3
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:507
-- @test: NumericForLoopTUnitTests.InvalidLimitReportedBeforeZeroStepThroughLua53
-- Lua 5.1-5.3 validate the limit before considering the tolerated zero step, with the
-- plain "must be a number" message.
for i = 1, {}, 0 do end
