-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1840
-- @test: MathModuleTUnitTests.ModfNegativeInfinityFractionalPartIsNegativeZero
-- Version-specific expected behavior:
--   Lua 5.1/5.2: is_neg_zero = true (fractional part is -0)
--   Lua 5.3+: is_neg_zero = false (fractional part is +0)
local int_part, frac_part = math.modf(-math.huge)
local is_neg_zero = (frac_part == 0 and 1 / frac_part == -math.huge)
return int_part, frac_part, is_neg_zero