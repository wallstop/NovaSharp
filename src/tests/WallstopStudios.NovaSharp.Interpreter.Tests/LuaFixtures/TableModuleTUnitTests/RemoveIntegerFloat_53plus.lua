-- @lua-versions: 5.3, 5.4
-- Tests that table.remove accepts float positions that have integer representation in Lua 5.3+
-- Per Lua 5.3 manual §6.6: 2.0 has integer representation

local t = {1, 2, 3}
local removed = table.remove(t, 2.0)  -- 2.0 has integer representation, should work
assert(removed == 2, "Removed element should be 2")
assert(#t == 2, "Table should have 2 elements")
print("PASS: table.remove(t, 2.0) accepted")
