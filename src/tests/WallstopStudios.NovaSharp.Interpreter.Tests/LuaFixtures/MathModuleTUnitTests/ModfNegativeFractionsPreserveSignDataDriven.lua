-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:2260
-- @test: MathModuleTUnitTests.ModfNegativeFractionsPreserveSignDataDriven
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local int_part, frac_part = math.modf({luaInput})
                local int_is_neg_zero = (int_part == 0 and 1/int_part == -math.huge)
                return int_part, frac_part, int_is_neg_zero
