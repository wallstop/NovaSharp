-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.GetUserValueLua54ReturnsFalseForNonUserData

-- Test: debug.getuservalue returns (nil, false) for non-userdata in Lua 5.4+
-- Reference: Lua 5.4 manual - getuservalue returns false if userdata doesn't have that value
-- Second return value indicates whether userdata has that value slot

local val, hasVal = debug.getuservalue("not userdata", 1)
local stringOk, stringVal, stringHasVal = pcall(
    debug.getuservalue,
    "not userdata",
    "1"
)
local fractionOk, fractionError = pcall(
    debug.getuservalue,
    "not userdata",
    1.5
)
local preciseOk, preciseValue, preciseHasValue = pcall(
    debug.getuservalue,
    "not userdata",
    "9007199254740993"
)
local maxIntegerOk, maxIntegerValue, maxIntegerHasValue = pcall(
    debug.getuservalue,
    "not userdata",
    "9223372036854775807"
)
local hexOk, hexValue, hexHasValue = pcall(
    debug.getuservalue,
    "not userdata",
    "0x100000001"
)
local signedHexOk, signedHexValue, signedHexHasValue = pcall(
    debug.getuservalue,
    "not userdata",
    "-0xffffffff"
)
local hexFloatOk, hexFloatValue, hexFloatHasValue = pcall(
    debug.getuservalue,
    "not userdata",
    "0x1p0"
)
local fullMaskOk, fullMaskValue, fullMaskHasValue = pcall(
    debug.getuservalue,
    "not userdata",
    "0xffffffffffffffff"
)
local negativeFullMaskOk, negativeFullMaskValue, negativeFullMaskHasValue = pcall(
    debug.getuservalue,
    "not userdata",
    "-0xffffffffffffffff"
)
local thousandsOk, thousandsError = pcall(
    debug.getuservalue,
    "not userdata",
    "1,000"
)

print("ordinary", val == nil, hasVal == nil)
print("numeric-string", stringOk, stringVal == nil, stringHasVal == nil)
print(
    "fraction",
    fractionOk,
    string.find(fractionError, "bad argument #2") ~= nil,
    string.find(fractionError, "integer representation") ~= nil
)
print("precise", preciseOk, preciseValue == nil, preciseHasValue == nil)
print("max-integer", maxIntegerOk, maxIntegerValue == nil, maxIntegerHasValue == nil)
print("hex", hexOk, hexValue == nil, hexHasValue == nil)
print("signed-hex", signedHexOk, signedHexValue == nil, signedHexHasValue == nil)
print("hex-float", hexFloatOk, hexFloatValue == nil, hexFloatHasValue == nil)
print("full-mask", fullMaskOk, fullMaskValue == nil, fullMaskHasValue == nil)
print(
    "negative-full-mask",
    negativeFullMaskOk,
    negativeFullMaskValue == nil,
    negativeFullMaskHasValue == nil
)
print(
    "thousands",
    thousandsOk,
    string.find(thousandsError, "bad argument #2") ~= nil,
    string.find(thousandsError, "number expected") ~= nil
)
return val == nil and hasVal == nil
