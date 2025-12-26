-- Tests that math.random(nan, n) does NOT throw in Lua 5.1
-- Lua 5.1 uses luaL_checkint() which converts to long FIRST, then compares
-- NaN converts to LONG_MIN in most C implementations
-- Comparison: LONG_MIN <= 10 is TRUE, so no error

-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomSucceedsOnNaNFirstArgLua51
local nan = 0 / 0
local result = math.random(nan, 10)
-- Should NOT throw in Lua 5.1, returns a number (garbage due to overflow)
print(type(result) == "number")