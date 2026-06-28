-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Compatibility\IntegerBoundaryTUnitTests.cs:773
-- @test: IntegerBoundaryTUnitTests.OriginalArm64DiscrepancyFixed
-- Test targets Lua 5.3+; Lua 5.3+: math.tointeger (5.3+)
return math.tointeger(2^63)
