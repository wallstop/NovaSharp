-- @lua-versions: 5.2, 5.3, 5.4
-- Tests that table.unpack accepts float indices that have integer representation in Lua 5.2+
-- Per Lua 5.3 manual §6.6: 2.0 and 4.0 have integer representation

local t = {1, 2, 3, 4, 5}
local a, b, c = table.unpack(t, 2.0, 4.0)  -- Both have integer representation
assert(a == 2 and b == 3 and c == 4, "Expected 2, 3, 4")
print("PASS: table.unpack(t, 2.0, 4.0) accepted")
