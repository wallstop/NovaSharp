-- bit32_unavailable_51.lua
-- @version: 5.1
-- @description: Verifies bit32 library is NOT available in Lua 5.1
-- @expected-result: returns true

-- In Lua 5.1, bit32 did not exist
-- bit32 should be nil
assert(bit32 == nil, "bit32 should be nil in Lua 5.1 (it was introduced in Lua 5.2)")

return true
