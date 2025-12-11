-- Test: Float modulo by zero returns nan in all Lua versions
-- Expected: PASS (returns nan)
-- Versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- Reference: §3.4.1 Arithmetic Operators (IEEE 754)

local result = 5.0 % 0.0

if result ~= result then  -- NaN is not equal to itself
    print("PASS")
else
    print("FAIL: Expected nan, got " .. tostring(result))
end
