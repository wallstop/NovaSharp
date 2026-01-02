-- bit32_unavailable_53plus.lua
-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: false
-- Verifies bit32 library is NOT available in Lua 5.4+ (removed)
-- In Lua 5.3, bit32 was deprecated but still available in standard builds.
-- In Lua 5.4+, bit32 was removed entirely.
-- NovaSharp follows reference Lua behavior: bit32 is nil in globals for 5.4+.

-- In Lua 5.4+, bit32 is NOT part of the standard library (fully removed)
-- bit32 should be nil in the global namespace
assert(bit32 == nil, "bit32 should be nil in Lua 5.4+ (removed)")

-- In Lua 5.3+, native bitwise operators are available instead
-- These operators work directly on integers
local a = 0xFF
local b = 0x0F

-- Test native bitwise AND
assert((a & b) == 0x0F, "native bitwise AND should work in Lua 5.3+")

-- Test native bitwise OR
assert((a | b) == 0xFF, "native bitwise OR should work in Lua 5.3+")

-- Test native bitwise XOR
assert((a ~ b) == 0xF0, "native bitwise XOR should work in Lua 5.3+")

-- Test native bitwise NOT
assert((~0) == -1, "native bitwise NOT should work in Lua 5.3+")

-- Test native left shift
assert((1 << 4) == 16, "native left shift should work in Lua 5.3+")

-- Test native right shift
assert((16 >> 2) == 4, "native right shift should work in Lua 5.3+")

return true