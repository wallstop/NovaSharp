-- Tests that math.random(n) accepts fractional values in Lua 5.1
-- Lua 5.1 silently truncates via floor; Lua 5.2 uses ceil (different behavior)
-- Lua 5.3+ requires integer representation
-- Note: _VERSION is not included in output since it differs between Lua and NovaSharp

-- This should succeed in 5.1/5.2 (fractional is truncated)
-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomIntegerRepresentation_51_52
local result = math.random(2.9)
assert(result >= 1 and result <= 2, "Result should be in [1, 2] range")
print("PASS: math.random(2.9) accepted, result in range [1, 2]")
