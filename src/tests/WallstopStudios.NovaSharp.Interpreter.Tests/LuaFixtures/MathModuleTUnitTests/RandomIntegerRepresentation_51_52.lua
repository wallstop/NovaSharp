-- Tests that math.random(n) accepts fractional values with truncation
-- Lua 5.1 silently truncates via floor; Lua 5.2 uses ceil (different behavior)
-- Lua 5.3+ requires integer representation
-- NovaSharp uses consistent floor truncation across all version modes
-- This test verifies NovaSharp's behavior, which may differ from reference Lua per-version

-- @lua-versions: 5.1
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomIntegerRepresentation_51_52
local result = math.random(2.9)
assert(result >= 1 and result <= 2, "Result should be in [1, 2] range")
print("PASS: math.random(2.9) accepted, result in range [1, 2]")
