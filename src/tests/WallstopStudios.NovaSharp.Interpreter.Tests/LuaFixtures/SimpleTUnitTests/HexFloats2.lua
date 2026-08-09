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
local isWindows = os.getenv("OS") == "Windows_NT" or os.getenv("WINDIR") ~= nil
-- Lua delegates hexadecimal conversion to the host C runtime. MSVCRT rejects the
-- extreme exponents and rounds the wide significand differently, while Unix libcs
-- agree with NovaSharp. Keep portable syntax coverage on Windows; the exact IEEE
-- behavior remains asserted by the platform-independent C# test.
if isWindows then
    print("hex-float-portable", normal == 0xA23 / 16)
else
    print(
        "hex-float",
        normal == 0xA23 / 16,
        overflow == math.huge,
        underflow == 0,
        compensated == 1,
        subnormal > 0 and subnormal / 2 == 0,
        string.format("%a", rounding)
    )
end
return normal, overflow, underflow, compensated, subnormal, rounding
