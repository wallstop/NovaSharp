-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:0
-- @test: BasicModuleTUnitTests.IpairsRequiresTableArgument
-- @compat-notes: ipairs() requires table argument in all Lua versions when no __ipairs metamethod exists

-- Test: ipairs() should error when called with non-table arguments
-- Reference: All Lua versions (5.1-5.5)

local function test_ipairs_nil()
    local ok, err = pcall(function() return ipairs(nil) end)
    assert(not ok, "ipairs(nil) should error")
    assert(err:find("table expected"), "Error should mention 'table expected'")
end

local function test_ipairs_number()
    local ok, err = pcall(function() return ipairs(123) end)
    assert(not ok, "ipairs(123) should error")
    assert(err:find("table expected"), "Error should mention 'table expected'")
end

local function test_ipairs_boolean()
    local ok, err = pcall(function() return ipairs(true) end)
    assert(not ok, "ipairs(true) should error")
    assert(err:find("table expected"), "Error should mention 'table expected'")
end

local function test_ipairs_string()
    local ok, err = pcall(function() return ipairs("hello") end)
    assert(not ok, 'ipairs("hello") should error')
    assert(err:find("table expected"), "Error should mention 'table expected'")
end

local function test_ipairs_function()
    local ok, err = pcall(function() return ipairs(function() end) end)
    assert(not ok, "ipairs(function) should error")
    assert(err:find("table expected"), "Error should mention 'table expected'")
end

local function test_ipairs_table_success()
    local ok = pcall(function() return ipairs({}) end)
    assert(ok, "ipairs({}) should succeed")
end

test_ipairs_nil()
test_ipairs_number()
test_ipairs_boolean()
test_ipairs_string()
test_ipairs_function()
test_ipairs_table_success()

print("PASS: ipairs() correctly requires table argument")
return "PASS"
