-- Tests that math.random(nan, n) THROWS in Lua 5.1/5.2
-- In reference Lua 5.1/5.2, nan <= 10 is FALSE per IEEE 754, so "interval is empty"

-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomErrorsOnNaNFirstArgLua51And52
local nan = 0/0
local result = math.random(nan, 10)
-- Should throw "interval is empty" in reference Lua 5.1/5.2
print("ERROR: Should have thrown")
