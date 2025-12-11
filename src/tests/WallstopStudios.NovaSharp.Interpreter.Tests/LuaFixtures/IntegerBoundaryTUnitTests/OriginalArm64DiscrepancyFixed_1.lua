-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Compatibility\IntegerBoundaryTUnitTests.cs:713
-- @test: IntegerBoundaryTUnitTests.OriginalArm64DiscrepancyFixed
-- @compat-notes: Lua 5.3+: math.tointeger (5.3+)
return math.tointeger(2^63)
