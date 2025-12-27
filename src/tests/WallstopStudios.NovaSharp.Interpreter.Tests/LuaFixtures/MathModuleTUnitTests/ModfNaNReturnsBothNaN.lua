-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:2310
-- @test: MathModuleTUnitTests.ModfNaNReturnsBothNaN
local int_part, frac_part = math.modf(0/0)
                return int_part, frac_part, int_part ~= int_part, frac_part ~= frac_part
