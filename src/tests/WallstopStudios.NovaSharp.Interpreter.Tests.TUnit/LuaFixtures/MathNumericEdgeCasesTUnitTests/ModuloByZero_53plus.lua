-- Test: Integer modulo by zero throws error in Lua 5.3+
-- Expected: ERROR (attempt to perform 'n%0')
-- Versions: 5.3, 5.4, 5.5
-- Reference: §3.4.1 Arithmetic Operators

-- @expects-error

local success, err = pcall(function()
    return 1 % 0
end)

if not success then
    if string.find(err, "n%%0") then
        print("PASS: Got expected error")
    else
        print("PASS: Got error (message: " .. tostring(err) .. ")")
    end
else
    print("FAIL: Expected error but got " .. tostring(err))
end
