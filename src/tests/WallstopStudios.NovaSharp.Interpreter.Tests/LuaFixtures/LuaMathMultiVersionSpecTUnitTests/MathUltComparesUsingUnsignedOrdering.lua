-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaMathMultiVersionSpecTUnitTests.cs:101
-- @test: LuaMathMultiVersionSpecTUnitTests.MathUltComparesUsingUnsignedOrdering
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.ult (5.3+)
return math.ult(0, -1), math.ult(-1, 0), math.ult(10, 20)
