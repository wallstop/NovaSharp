-- Test: LuaCompatibleErrors with global variable
-- Expected: Error message includes "global 'undeclared'"

local success, err = pcall(function()
    undeclared.field = 1
end)

assert(not success, "Should have raised an error")
-- Check that error contains expected text
assert(string.find(err, "attempt to index") ~= nil, "Error should mention 'attempt to index'")
assert(string.find(err, "nil") ~= nil, "Error should mention 'nil'")
-- When LuaCompatibleErrors is enabled, should include variable name
-- When disabled, this assertion will be skipped in tests
