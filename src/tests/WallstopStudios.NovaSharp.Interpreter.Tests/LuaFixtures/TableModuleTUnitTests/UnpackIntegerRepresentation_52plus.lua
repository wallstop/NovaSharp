-- @lua-versions: 5.2, 5.3, 5.4
-- @expects-error: true
-- Tests that table.unpack(t, i, j) requires integer representation for indices in Lua 5.2+
-- Per Lua 5.3 manual §6.6: table.unpack indices must have integer representation
-- NOTE: Lua 5.1 does not have table.unpack (uses global unpack instead)

-- table.unpack was added in Lua 5.2
local t = {1, 2, 3, 4, 5}
table.unpack(t, 1.5, 3)
print("ERROR: Should have thrown")
