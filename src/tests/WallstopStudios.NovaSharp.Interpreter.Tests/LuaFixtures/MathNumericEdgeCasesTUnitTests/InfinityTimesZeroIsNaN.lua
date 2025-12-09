-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:271
-- @test: MathNumericEdgeCasesTUnitTests.InfinityTimesZeroIsNaN
-- @compat-notes: Lua 5.3+: bitwise operators
local inf = 1/0; return inf * 0
