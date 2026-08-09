-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:548
-- @test: BasicModuleTUnitTests.ToNumberReturnsNilForInvalidHexString
local invalidHex = tonumber("0x")
local thousands = tonumber("1,000")
local trailingPoint = tonumber("1.")
local exponent = tonumber("1e0")
local overflow = tonumber("1e999999")
local underflow = tonumber("1e-999999")
local highDecimal = tonumber("9223372036854775807")
local highDecimalPlusOne = highDecimal + 1
local negativeZero = tonumber("-0")
local hexOverflow = tonumber("0x1p999999999999")
local hexUnderflow = tonumber("0x1p-999999999999")
local compensatedHex = tonumber("0x" .. string.rep("f", 400) .. "p-1600")
local subnormalHex = tonumber("0xffffffffffffffffp-1138")
local roundingHex = tonumber("0x220e087835b925585p376")
local roundingHexInteger = tonumber("0x220e087835b925585")
local unicodeExponent = tonumber("0x1p١")
print("invalid", invalidHex == nil)
print("thousands", thousands == nil)
print(
    "decimal",
    math.type and math.type(trailingPoint) or "float",
    math.type and math.type(exponent) or "float",
    overflow == math.huge,
    underflow == 0,
    math.type and math.type(underflow) or "float",
    highDecimalPlusOne > 0
)
print("negative-zero", 1 / negativeZero == -math.huge)
if _VERSION ~= "Lua 5.1" then
    print(
        "hex-exponent",
        hexOverflow == nil,
        hexOverflow == math.huge,
        hexUnderflow == nil,
        hexUnderflow == 0,
        compensatedHex == nil,
        compensatedHex == 1,
        subnormalHex > 0 and subnormalHex / 2 == 0,
        string.format("%a", roundingHex),
        unicodeExponent == nil
    )
    if _VERSION == "Lua 5.2" then
        print("hex-integer-rounding", string.format("%a", roundingHexInteger))
    end
end
return invalidHex == nil and thousands == nil
