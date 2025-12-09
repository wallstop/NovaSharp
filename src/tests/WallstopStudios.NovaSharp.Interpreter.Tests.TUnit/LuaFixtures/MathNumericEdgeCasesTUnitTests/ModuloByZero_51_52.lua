-- Test: Integer modulo by zero returns nan in Lua 5.1/5.2
-- Expected: PASS (returns nan)
-- Versions: 5.1, 5.2
-- Reference: §3.4.1 Arithmetic Operators

local result = 1 % 0

if result ~= result then  -- NaN is not equal to itself
    print("PASS")
else
    print("FAIL: Expected nan, got " .. tostring(result))
end
