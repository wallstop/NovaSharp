-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaMathMultiVersionSpecTUnitTests.cs:101
-- @test: LuaMathMultiVersionSpecTUnitTests.MathUltComparesUsingUnsignedOrdering
-- Test targets Lua 5.3+; Lua 5.3+: math.ult (5.3+)
return math.ult(0, -1), math.ult(-1, 0), math.ult(10, 20)
