-- Tests that math.random(inf, n) does NOT throw in Lua 5.1/5.2
-- In reference Lua 5.1/5.2 on macOS, (long)infinity produces LONG_MIN,
-- so LONG_MIN <= n is true, which means the interval is valid and returns a number

-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomSucceedsOnPositiveInfinityFirstArgLua51And52
local inf = 1/0
local result = math.random(inf, 10)
-- Should not throw, should return a number
print(type(result) == "number")
