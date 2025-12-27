-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1786
-- @test: MathModuleTUnitTests.ModfPreservesNegativeZeroForNegativeIntegers
local int_part, frac_part = math.modf(-5)
                -- Check if fractional part is negative zero (1/-0 = -inf)
                local is_neg_zero = (frac_part == 0 and 1/frac_part == -math.huge)
                return int_part, frac_part, is_neg_zero
