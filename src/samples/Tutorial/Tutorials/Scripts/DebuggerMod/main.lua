local function xor_demo(a, b)
  -- bit32 only ships with Lua 5.2 profiles on NovaSharp.
  return bit32.bxor(a, b)
end

local lhs = 0xFF00
local rhs = 0xF0F0
local result = xor_demo(lhs, rhs)

print(string.format("bit32.bxor(0x%X, 0x%X) -> 0x%X", lhs, rhs, result))

return result
