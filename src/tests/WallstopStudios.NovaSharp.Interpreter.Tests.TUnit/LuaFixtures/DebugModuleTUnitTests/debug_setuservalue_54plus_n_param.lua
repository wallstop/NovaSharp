-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.SetUserValueLua54WithNParameterSlot1Works

-- Test: debug.setuservalue with n parameter works for slot 1 in Lua 5.4+
-- Reference: Lua 5.4 manual - setuservalue(udata, value, n) sets n-th user value
-- n parameter is 1-based, n=1 is first user value slot

-- These calls use non-userdata so reference Lua can verify n coercion and
-- validation order without requiring host-provided userdata.
local stringOk, stringError = pcall(
    debug.setuservalue,
    "not userdata",
    {},
    "1"
)
local fractionOk, fractionError = pcall(
    debug.setuservalue,
    "not userdata",
    {},
    1.5
)
local thousandsOk, thousandsError = pcall(
    debug.setuservalue,
    "not userdata",
    {},
    "1,000"
)

print(
    "numeric-string",
    stringOk,
    string.find(stringError, "bad argument #1") ~= nil
)
print(
    "fraction",
    fractionOk,
    string.find(fractionError, "bad argument #3") ~= nil,
    string.find(fractionError, "integer representation") ~= nil
)
print(
    "thousands",
    thousandsOk,
    string.find(thousandsError, "bad argument #3") ~= nil,
    string.find(thousandsError, "number expected") ~= nil
)
return not stringOk and not fractionOk and not thousandsOk
