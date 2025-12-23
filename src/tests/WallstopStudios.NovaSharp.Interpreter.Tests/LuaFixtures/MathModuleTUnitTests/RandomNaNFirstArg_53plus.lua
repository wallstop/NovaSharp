-- Tests that math.random(nan, n) throws in Lua 5.3+
-- NaN has no integer representation

-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomRejectsInfinityAndNaNLua53Plus
local nan = 0/0
math.random(nan, 10)
print("ERROR: Should have thrown")
