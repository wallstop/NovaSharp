-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false

-- Test that string.format("%s", ...) in Lua 5.1 requires string/number arguments
-- Booleans, nil, tables, and functions are rejected with "string expected" errors

-- Test 1: string argument (should work)
local result = string.format("%s", "hello")
assert(result == "hello", "string: expected 'hello', got '" .. result .. "'")

-- Test 2: number argument (Lua 5.1 auto-converts numbers to strings for %s)
result = string.format("%s", 123)
assert(result == "123", "number: expected '123', got '" .. result .. "'")

-- Test 3: negative number
result = string.format("%s", -42)
assert(result == "-42", "negative: expected '-42', got '" .. result .. "'")

-- Test 4: floating point number
result = string.format("%s", 3.14)
assert(result == "3.14", "float: expected '3.14', got '" .. result .. "'")

-- Test 5: zero
result = string.format("%s", 0)
assert(result == "0", "zero: expected '0', got '" .. result .. "'")

-- Test 6: multiple string/number arguments
result = string.format("%s %s", 1, 2)
assert(result == "1 2", "multiple numbers: expected '1 2', got '" .. result .. "'")

result = string.format("%s %s %s", "a", 42, "b")
assert(result == "a 42 b", "mixed string/number: expected 'a 42 b', got '" .. result .. "'")

-- Test 7: boolean true should FAIL in 5.1
local ok, err = pcall(function() return string.format("%s", true) end)
assert(not ok, "boolean true should fail in Lua 5.1")
assert(string.find(err, "string expected") or string.find(err, "got boolean"), 
       "error should mention 'string expected' or 'got boolean', got: " .. tostring(err))

-- Test 8: boolean false should FAIL in 5.1
ok, err = pcall(function() return string.format("%s", false) end)
assert(not ok, "boolean false should fail in Lua 5.1")
assert(string.find(err, "string expected") or string.find(err, "got boolean"), 
       "error should mention 'string expected' or 'got boolean'")

-- Test 9: nil should FAIL in 5.1
ok, err = pcall(function() return string.format("%s", nil) end)
assert(not ok, "nil should fail in Lua 5.1")
assert(string.find(err, "string expected") or string.find(err, "got nil"), 
       "error should mention 'string expected' or 'got nil'")

-- Test 10: table should FAIL in 5.1
ok, err = pcall(function() return string.format("%s", {}) end)
assert(not ok, "table should fail in Lua 5.1")
assert(string.find(err, "string expected") or string.find(err, "got table"), 
       "error should mention 'string expected' or 'got table'")

-- Test 11: function should FAIL in 5.1
ok, err = pcall(function() return string.format("%s", function() end) end)
assert(not ok, "function should fail in Lua 5.1")
assert(string.find(err, "string expected") or string.find(err, "got function"), 
       "error should mention 'string expected' or 'got function'")

-- Test 12: mixed format where 4th argument is boolean should FAIL
ok, err = pcall(function() return string.format("%s %d %s", "a", 42, true) end)
assert(not ok, "mixed format with boolean for %s should fail in Lua 5.1")
assert(string.find(err, "string expected") or string.find(err, "got boolean"), 
       "error should mention 'string expected' or 'got boolean'")

-- Test 13: userdata would also fail, but we can't easily create one in pure Lua
-- (Skipping userdata test as it requires C API or io.stdin which varies)

print("PASS: All Lua 5.1 string.format('%s', ...) strict typing tests passed")
