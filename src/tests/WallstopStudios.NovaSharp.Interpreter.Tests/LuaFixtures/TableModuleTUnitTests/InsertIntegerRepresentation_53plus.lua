-- @lua-versions: 5.3, 5.4
-- @expects-error: true
-- Tests that table.insert(t, pos, value) requires integer representation for position in Lua 5.3+
-- Per Lua 5.3 manual §6.6: table.insert position must have integer representation

local t = {1, 2, 3}
table.insert(t, 1.5, "x")
print("ERROR: Should have thrown")
