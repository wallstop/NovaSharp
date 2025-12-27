-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1840
-- @test: MathModuleTUnitTests.ModfNegativeInfinityFractionalPartIsNegativeZero
local int_part, frac_part = math.modf(-math.huge)
                local is_neg_zero = (frac_part == 0 and 1/frac_part == -math.huge)
                return int_part, frac_part, is_neg_zero
