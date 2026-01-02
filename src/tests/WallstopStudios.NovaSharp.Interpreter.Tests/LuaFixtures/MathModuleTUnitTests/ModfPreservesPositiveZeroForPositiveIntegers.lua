-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:1868
-- @test: MathModuleTUnitTests.ModfPreservesPositiveZeroForPositiveIntegers
-- @compat-notes: Test targets Lua 5.3+
local int_part, frac_part = math.modf(5)
                -- Check if fractional part is positive zero (1/+0 = +inf)
                local is_pos_zero = (frac_part == 0 and 1/frac_part == math.huge)
                return int_part, frac_part, is_pos_zero
