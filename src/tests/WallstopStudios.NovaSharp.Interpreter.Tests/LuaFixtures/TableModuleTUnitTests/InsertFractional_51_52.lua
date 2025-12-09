-- @lua-versions: 5.1, 5.2
-- Tests that table.insert(t, pos, value) accepts fractional position in Lua 5.1/5.2
-- These versions silently truncate via floor

local t = {1, 2, 3}
table.insert(t, 2.9, "x")  -- Should insert at position 2
assert(t[1] == 1, "First element should still be 1")
assert(t[2] == "x", "Inserted element should be at position 2")
assert(t[3] == 2, "Old element at 2 should move to 3")
print("PASS: table.insert with fractional position accepted")
