-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Spec/LuaMathMultiVersionSpecTUnitTests.cs:90
-- @test: LuaMathMultiVersionSpecTUnitTests.MathUltComparesUsingUnsignedOrderingAcrossLua53PlusVersions
-- @compat-notes: Test targets Lua 5.3+
return math.ult(0, -1), math.ult(-1, 0), math.ult(10, 20)
