-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:2125
-- @test: MathModuleTUnitTests.ModfPositiveIntegersPreservePositiveZeroDataDriven
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local int_part, frac_part = math.modf({input})
                local is_pos_zero = (frac_part == 0 and 1/frac_part == math.huge)
                return int_part, frac_part, is_pos_zero
