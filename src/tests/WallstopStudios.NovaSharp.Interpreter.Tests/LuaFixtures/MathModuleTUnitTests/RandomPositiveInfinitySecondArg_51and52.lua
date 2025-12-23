-- Tests that math.random(m, inf) THROWS in Lua 5.1/5.2
-- In reference Lua 5.1/5.2 on macOS, (long)infinity produces LONG_MIN,
-- so m > LONG_MIN is true for positive m, triggering "interval is empty"

-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomErrorsOnPositiveInfinitySecondArgLua51And52
local inf = 1/0
local result = math.random(1, inf)
-- Should throw "interval is empty" in reference Lua 5.1/5.2
print("ERROR: Should have thrown")
