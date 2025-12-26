-- Tests that math.random(inf, n) does NOT throw in Lua 5.1
-- Lua 5.1 uses luaL_checkint() which converts to long FIRST, then compares
-- Infinity converts to LONG_MIN in most C implementations
-- Comparison: LONG_MIN <= 10 is TRUE, so no error

-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomSucceedsOnPositiveInfinityFirstArgLua51
local inf = 1 / 0
local result = math.random(inf, 10)
-- Should NOT throw in Lua 5.1, returns a number (garbage due to overflow)
print(type(result) == "number")