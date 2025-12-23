-- Tests that math.random(nan) THROWS in Lua 5.1/5.2
-- In reference Lua 5.1/5.2 on macOS, (long)NaN produces LONG_MIN,
-- which fails the u < 1 check and throws "interval is empty"

-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomErrorsOnNaNLua51And52
local nan = 0/0
local result = math.random(nan)
-- Should throw "interval is empty" in reference Lua 5.1/5.2
print("ERROR: Should have thrown")
