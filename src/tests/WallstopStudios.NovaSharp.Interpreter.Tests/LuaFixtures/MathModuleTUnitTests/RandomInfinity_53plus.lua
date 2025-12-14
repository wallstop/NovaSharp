-- Tests that math.random(n) rejects infinity in Lua 5.3+
-- Infinity has no integer representation

-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomInfinity_53plus
local inf = 1/0
math.random(inf)
print("ERROR: Should have thrown")
