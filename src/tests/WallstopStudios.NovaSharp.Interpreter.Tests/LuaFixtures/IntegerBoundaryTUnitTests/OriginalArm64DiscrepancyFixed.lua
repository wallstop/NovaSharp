-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/IntegerBoundaryTUnitTests.cs:694
-- @test: IntegerBoundaryTUnitTests.OriginalArm64DiscrepancyFixed
-- @compat-notes: Lua 5.3+: math.ult (5.3+); Lua 5.3+: math.maxinteger (5.3+); Lua 5.3+: math.mininteger (5.3+)
return math.ult(math.maxinteger, math.mininteger)
