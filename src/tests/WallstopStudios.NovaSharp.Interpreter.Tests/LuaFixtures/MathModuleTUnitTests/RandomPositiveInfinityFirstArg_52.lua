-- Tests that math.random(inf, n) THROWS in Lua 5.2
-- Lua 5.2 uses luaL_checknumber() which keeps values as floats for comparison
-- Comparison: inf <= 10 is FALSE per IEEE 754, so "interval is empty"

-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomErrorsOnPositiveInfinityFirstArgLua52
local inf = 1 / 0
local result = math.random(inf, 10)
-- Should throw "interval is empty" in Lua 5.2
print("ERROR: Should have thrown")