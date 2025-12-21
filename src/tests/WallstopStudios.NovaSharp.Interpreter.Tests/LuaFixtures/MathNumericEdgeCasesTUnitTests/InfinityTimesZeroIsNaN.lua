-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:427
-- @test: MathNumericEdgeCasesTUnitTests.InfinityTimesZeroIsNaN
-- @compat-notes: Test targets Lua 5.1
return math.huge * 0
