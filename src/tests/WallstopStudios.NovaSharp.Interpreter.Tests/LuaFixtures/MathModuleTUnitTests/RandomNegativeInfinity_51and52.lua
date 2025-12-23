-- Tests that math.random(-inf) does NOT throw in Lua 5.1/5.2
-- In reference Lua 5.1/5.2, negative infinity is silently handled (not an error)

-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomAcceptsNegativeInfinityLua51And52
local neginf = -1/0
local result = math.random(neginf)
-- Should not throw, should return a number
print(type(result) == "number")
