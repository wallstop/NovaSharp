-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:0
-- @test: BasicModuleTUnitTests.PairsRequiresTableArgument
-- @compat-notes: pairs() requires table argument in all Lua versions when no __pairs metamethod exists

-- Test: pairs() should error when called with non-table arguments
-- Reference: All Lua versions (5.1-5.5)

local function test_pairs_nil()
    local ok, err = pcall(function() return pairs(nil) end)
    assert(not ok, "pairs(nil) should error")
    assert(err:find("table expected"), "Error should mention 'table expected'")
end

local function test_pairs_number()
    local ok, err = pcall(function() return pairs(123) end)
    assert(not ok, "pairs(123) should error")
    assert(err:find("table expected"), "Error should mention 'table expected'")
end

local function test_pairs_boolean()
    local ok, err = pcall(function() return pairs(true) end)
    assert(not ok, "pairs(true) should error")
    assert(err:find("table expected"), "Error should mention 'table expected'")
end

local function test_pairs_string()
    local ok, err = pcall(function() return pairs("hello") end)
    assert(not ok, 'pairs("hello") should error')
    assert(err:find("table expected"), "Error should mention 'table expected'")
end

local function test_pairs_function()
    local ok, err = pcall(function() return pairs(function() end) end)
    assert(not ok, "pairs(function) should error")
    assert(err:find("table expected"), "Error should mention 'table expected'")
end

local function test_pairs_table_success()
    local ok = pcall(function() return pairs({}) end)
    assert(ok, "pairs({}) should succeed")
end

test_pairs_nil()
test_pairs_number()
test_pairs_boolean()
test_pairs_string()
test_pairs_function()
test_pairs_table_success()

print("PASS: pairs() correctly requires table argument")
return "PASS"
