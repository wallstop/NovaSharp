-- @lua-versions: 5.1, 5.2, 5.3, 5.4
-- Tests that math.random throws "wrong number of arguments" when called with more than 2 arguments
-- Reference Lua behavior: all versions throw this error for math.random(1, 2, 3)

local ok, err = pcall(function() return math.random(1, 2, 3) end)

assert(not ok, "Expected math.random(1, 2, 3) to throw an error")
assert(string.find(err, "wrong number of arguments"),
  "Expected error message to contain 'wrong number of arguments', got: " .. tostring(err))

print("PASS: math.random(1, 2, 3) correctly throws 'wrong number of arguments'")