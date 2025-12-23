-- Tests that math.random(m, inf) does NOT throw in Lua 5.1/5.2
-- In reference Lua 5.1/5.2, infinity is silently handled (not an error)

-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomAcceptsInfinityAndNaNLua51And52
local inf = 1/0
local result = math.random(1, inf)
-- Should not throw, should return a number
print(type(result) == "number")
