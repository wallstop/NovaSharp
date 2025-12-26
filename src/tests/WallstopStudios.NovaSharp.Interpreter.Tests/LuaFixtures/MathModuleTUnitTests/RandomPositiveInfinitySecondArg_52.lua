-- Tests that math.random(m, inf) does NOT throw in Lua 5.2
-- Lua 5.2 uses luaL_checknumber() which keeps values as floats for comparison
-- Comparison: 1.0 <= inf is TRUE per IEEE 754, so interval check passes
-- The function proceeds and produces garbage output due to (long)infinity overflow

-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomSucceedsOnPositiveInfinitySecondArgLua52
local inf = 1 / 0
local result = math.random(1, inf)
-- Should NOT throw in Lua 5.2, returns a number (garbage due to overflow)
print(type(result) == "number")