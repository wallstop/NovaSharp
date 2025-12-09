-- @lua-versions: 5.3, 5.4
-- @expects-error: true
-- Tests that table.move(a1, f, e, t, a2) requires integer representation for second index in Lua 5.3+
-- Per Lua 5.3 manual §6.6: table.move indices must have integer representation

local a1 = {1, 2, 3, 4, 5}
local a2 = {}
table.move(a1, 1, 3.5, 1, a2)
print("ERROR: Should have thrown")
