-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1777
-- @test: SimpleTUnitTests.HexFloats2
local normal = 0xA23p-4
local overflow = 0x1p999999999999
local underflow = 0x1p-999999999999
local compensated = 0xffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffp-1600
local subnormal = 0xffffffffffffffffp-1138
local rounding = 0x220e087835b925585p376
print(
    "hex-float",
    normal == 0xA23 / 16,
    overflow == math.huge,
    underflow == 0,
    compensated == 1,
    subnormal > 0 and subnormal / 2 == 0,
    string.format("%a", rounding)
)
return normal, overflow, underflow, compensated, subnormal, rounding
