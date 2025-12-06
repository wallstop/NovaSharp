-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Spec/LuaMathMultiVersionSpecTUnitTests.cs:58
-- @test: LuaMathMultiVersionSpecTUnitTests.MathToIntegerConvertsNumbersAndStringsAcrossLua53PlusVersions
-- @compat-notes: Test targets Lua 5.3+
return math.tointeger(10.0), math.tointeger(-3), math.tointeger('42'), math.tointeger(3.25)
