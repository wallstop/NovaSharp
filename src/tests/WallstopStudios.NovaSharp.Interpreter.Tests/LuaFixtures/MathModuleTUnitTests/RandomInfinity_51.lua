-- Tests that math.random(inf) THROWS in Lua 5.1
-- Lua 5.1 uses luaL_checkint() which converts to long FIRST, then compares
-- Infinity converts to LONG_MIN in most C implementations
-- Comparison: 1 <= LONG_MIN is FALSE, so "interval is empty"

-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomErrorsOnInfinityLua51
local inf = 1 / 0
local result = math.random(inf)
-- Should throw "interval is empty" in Lua 5.1
print("ERROR: Should have thrown")