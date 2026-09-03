-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:530
-- @test: NumericForLoopTUnitTests.ZeroStepReportedBeforeInvalidLimitFromLua54
-- Lua 5.4+ reject the zero integer step before validating the limit.
for i = 1, {}, 0 do end
