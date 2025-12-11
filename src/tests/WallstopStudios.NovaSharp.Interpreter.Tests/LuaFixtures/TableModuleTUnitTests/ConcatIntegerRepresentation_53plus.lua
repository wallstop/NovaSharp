-- @lua-versions: 5.3, 5.4
-- @expects-error: true
-- Tests that table.concat(t, sep, i, j) requires integer representation for indices in Lua 5.3+
-- Per Lua 5.3 manual §6.6: table.concat indices must have integer representation

local t = {"a", "b", "c"}
table.concat(t, ",", 1.5, 3)
print("ERROR: Should have thrown")
