-- bit32_available_52.lua
-- @version: 5.2
-- @description: Verifies bit32 library is available as a global in Lua 5.2
-- @expected-result: returns true

-- In Lua 5.2, bit32 is part of the standard library
-- bit32 should be a table with the bitwise operation functions
assert(type(bit32) == "table", "bit32 should be a table in Lua 5.2")
assert(type(bit32.band) == "function", "bit32.band should be a function")
assert(type(bit32.bor) == "function", "bit32.bor should be a function")
assert(type(bit32.bxor) == "function", "bit32.bxor should be a function")
assert(type(bit32.bnot) == "function", "bit32.bnot should be a function")
assert(type(bit32.lshift) == "function", "bit32.lshift should be a function")
assert(type(bit32.rshift) == "function", "bit32.rshift should be a function")

-- Verify basic operations work
assert(bit32.band(0xFF, 0x0F) == 0x0F, "bit32.band should work")
assert(bit32.bor(0xF0, 0x0F) == 0xFF, "bit32.bor should work")
assert(bit32.bxor(0xFF, 0x0F) == 0xF0, "bit32.bxor should work")

return true
