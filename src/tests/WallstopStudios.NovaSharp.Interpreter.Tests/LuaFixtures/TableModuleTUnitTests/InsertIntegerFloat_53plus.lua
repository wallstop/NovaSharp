-- @lua-versions: 5.3, 5.4
-- Tests that table.insert accepts float positions that have integer representation in Lua 5.3+
-- Per Lua 5.3 manual §6.6: 2.0 has integer representation

local t = {1, 2, 3}
table.insert(t, 2.0, "x")  -- 2.0 has integer representation, should work
assert(t[1] == 1, "First element should still be 1")
assert(t[2] == "x", "Inserted element should be at position 2")
assert(t[3] == 2, "Old element at 2 should move to 3")
print("PASS: table.insert(t, 2.0, value) accepted")
