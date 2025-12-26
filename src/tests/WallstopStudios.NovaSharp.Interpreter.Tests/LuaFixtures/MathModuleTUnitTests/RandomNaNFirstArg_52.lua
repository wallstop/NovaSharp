-- Tests that math.random(nan, n) THROWS in Lua 5.2
-- Lua 5.2 uses luaL_checknumber() which keeps values as floats for comparison
-- Comparison: nan <= 10 is FALSE per IEEE 754, so "interval is empty"

-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomErrorsOnNaNFirstArgLua52
local nan = 0 / 0
local result = math.random(nan, 10)
-- Should throw "interval is empty" in Lua 5.2
print("ERROR: Should have thrown")