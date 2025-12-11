-- @lua-versions: 5.3, 5.4
-- Tests that table.concat accepts float indices that have integer representation in Lua 5.3+
-- Per Lua 5.3 manual §6.6: 1.0 and 2.0 have integer representation

local t = {"a", "b", "c"}
local result = table.concat(t, ",", 1.0, 2.0)  -- Both have integer representation
assert(result == "a,b", "Expected 'a,b', got: " .. result)
print("PASS: table.concat(t, sep, 1.0, 2.0) accepted")
