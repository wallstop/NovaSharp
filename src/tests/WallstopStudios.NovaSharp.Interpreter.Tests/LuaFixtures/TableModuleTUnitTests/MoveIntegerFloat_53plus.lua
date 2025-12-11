-- @lua-versions: 5.3, 5.4
-- Tests that table.move accepts float indices that have integer representation in Lua 5.3+
-- Per Lua 5.3 manual §6.6: 1.0, 3.0 have integer representation

local a1 = {1, 2, 3, 4, 5}
local a2 = {}
table.move(a1, 1.0, 3.0, 1.0, a2)  -- All have integer representation
assert(a2[1] == 1 and a2[2] == 2 and a2[3] == 3, "Expected 1, 2, 3")
print("PASS: table.move with integer floats accepted")
