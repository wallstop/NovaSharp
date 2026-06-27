-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaMathMultiVersionSpecTUnitTests.cs:59
-- @test: LuaMathMultiVersionSpecTUnitTests.MathToIntegerConvertsNumbersAndStringsAcrossLua53PlusVersions
-- Test targets Lua 5.3+; Lua 5.3+: math.tointeger (5.3+)
return math.tointeger(10.0), math.tointeger(-3), math.tointeger('42'), math.tointeger(3.25)
