-- Tests that math.random(m, -inf) throws in Lua 5.3+
-- Negative infinity has no integer representation

-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomRejectsInfinityAndNaNLua53Plus
local neginf = -1/0
math.random(1, neginf)
print("ERROR: Should have thrown")
