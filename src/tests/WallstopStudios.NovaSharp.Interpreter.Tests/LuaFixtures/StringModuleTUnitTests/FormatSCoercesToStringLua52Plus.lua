-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false

-- Test that string.format("%s", ...) auto-converts non-string arguments in Lua 5.2+
-- In Lua 5.2 and later, %s automatically applies tostring() to the argument

-- Test 1: number (also works in 5.1, but included for completeness)
local result = string.format("%s", 123)
assert(result == "123", "number: expected '123', got '" .. result .. "'")

-- Test 2: boolean true
result = string.format("%s", true)
assert(result == "true", "true: expected 'true', got '" .. result .. "'")

-- Test 3: boolean false
result = string.format("%s", false)
assert(result == "false", "false: expected 'false', got '" .. result .. "'")

-- Test 4: nil
result = string.format("%s", nil)
assert(result == "nil", "nil: expected 'nil', got '" .. result .. "'")

-- Test 5: table (result contains "table:" prefix)
local t = {}
result = string.format("%s", t)
assert(string.sub(result, 1, 6) == "table:", "table: expected 'table:...' prefix, got '" .. result .. "'")

-- Test 6: function (result contains "function:" prefix)
local f = function() end
result = string.format("%s", f)
assert(string.sub(result, 1, 9) == "function:", "function: expected 'function:...' prefix, got '" .. result .. "'")

-- Test 7: multiple non-string arguments
result = string.format("%s %s", 1, 2)
assert(result == "1 2", "multiple numbers: expected '1 2', got '" .. result .. "'")

result = string.format("%s %s %s", true, false, nil)
assert(result == "true false nil", "multiple booleans/nil: expected 'true false nil', got '" .. result .. "'")

-- Test 8: string argument (baseline - works in all versions)
result = string.format("%s", "hello")
assert(result == "hello", "string: expected 'hello', got '" .. result .. "'")

-- Test 9: mixed format with %s accepting non-strings
result = string.format("%s %d %s", "a", 42, true)
assert(result == "a 42 true", "mixed: expected 'a 42 true', got '" .. result .. "'")

-- Test 10: empty table and nested function
result = string.format("Value: %s", {})
assert(string.sub(result, 1, 13) == "Value: table:", "prefixed table: expected 'Value: table:...', got '" .. result .. "'")

-- Test 11: negative numbers and zero
result = string.format("%s %s %s", -42, 0, 3.14159)
assert(result == "-42 0 3.14159", "negative/zero/float: expected '-42 0 3.14159', got '" .. result .. "'")

-- Test 12: integer format specifier with boolean should fail (only %s coerces)
local ok, err = pcall(function() return string.format("%d", true) end)
assert(not ok, "%d with boolean should fail")
assert(string.find(err, "number expected") or string.find(err, "expected"), "%d error should mention 'number expected'")

print("PASS: All Lua 5.2+ string.format('%s', ...) coercion tests passed")
