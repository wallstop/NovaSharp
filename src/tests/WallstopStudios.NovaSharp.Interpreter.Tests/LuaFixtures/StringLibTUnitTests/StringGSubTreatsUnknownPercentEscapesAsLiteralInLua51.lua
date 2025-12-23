-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- Test: string.gsub treats unknown %escapes as literal characters in Lua 5.1
-- In Lua 5.1, %e in replacement string is treated as literal 'e'
-- Lua 5.2+ rejects such escapes with "invalid use of '%' in replacement string"
local result, count = string.gsub('hello world', '%w+', '%e')
assert(result == 'e e', "Expected 'e e' but got: " .. tostring(result))
assert(count == 2, "Expected 2 replacements but got: " .. tostring(count))
print("PASS")
