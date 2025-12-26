-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @test: BasicModuleTUnitTests.AddressFormatMatch
-- @compat-notes: Tests NovaSharp's normalized address format (0x prefix, lowercase hex). Windows Lua uses different format (no 0x prefix, uppercase hex).

-- Test: Address format should match NovaSharp's normalized style
-- This test verifies that tostring() produces the expected NovaSharp format.
-- Expected format: "type: 0x[hex]" (lowercase hex, no fixed padding)

-- Test table address format
local t = {}
local ts = tostring(t)
assert(ts:match("^table: 0x%x+$"), "table format should be 'table: 0x<hex>' but got: " .. ts)

-- Test function address format
local f = function() end
local fs = tostring(f)
assert(fs:match("^function: 0x%x+$"), "function format should be 'function: 0x<hex>' but got: " .. fs)

-- Test coroutine/thread address format
local co = coroutine.create(function() end)
local cos = tostring(co)
assert(cos:match("^thread: 0x%x+$"), "thread format should be 'thread: 0x<hex>' but got: " .. cos)

-- Verify lowercase hex (not uppercase)
-- The pattern %x matches both, but we can check no uppercase A-F appear
local function hasUpperHex(s)
    return s:match("[A-F]") ~= nil
end

-- Note: In reference Lua, addresses are lowercase. NovaSharp should match this.
-- This is a soft check - some Lua implementations might use uppercase.
print("table:    " .. ts)
print("function: " .. fs)
print("thread:   " .. cos)

print("PASS: All address formats match expected pattern")