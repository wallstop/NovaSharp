-- Test: setfenv error cases
-- Expected: success
-- Description: Tests setfenv error handling

-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.setfenv_errors_51
local saved_print = print

-- Test: setfenv with invalid first argument type (string)
local ok1, err1 = pcall(function() setfenv("string", {}) end)
assert(not ok1, "setfenv(string, {}) should throw error")
assert(err1:find("number expected"), "Error should say 'number expected', got: " .. tostring(err1))

-- Test: setfenv with invalid second argument type (string)
local f = function() end
local ok2, err2 = pcall(function() setfenv(f, "string") end)
assert(not ok2, "setfenv(f, string) should throw error")
assert(err2:find("table expected"), "Error should say 'table expected', got: " .. tostring(err2))

-- Test: setfenv with nil second argument
local ok3, err3 = pcall(function() setfenv(f, nil) end)
assert(not ok3, "setfenv(f, nil) should throw error")
assert(err3:find("table expected"), "Error should say 'table expected', got: " .. tostring(err3))

-- Test: setfenv with invalid level
local ok4, err4 = pcall(function() setfenv(100, {}) end)
assert(not ok4, "setfenv(100, {}) should throw error")
assert(err4:find("invalid level"), "Error should mention 'invalid level', got: " .. tostring(err4))

-- Test: setfenv with negative level
local ok5, err5 = pcall(function() setfenv(-1, {}) end)
assert(not ok5, "setfenv(-1, {}) should throw error")
assert(err5:find("non-negative") or err5:find("negative"), "Error should mention negative level, got: " .. tostring(err5))

-- Test: setfenv on C function should fail
local ok6, err6 = pcall(function() setfenv(print, {}) end)
assert(not ok6, "setfenv(print, {}) should throw error")
assert(err6:find("cannot change"), "Error should say 'cannot change', got: " .. tostring(err6))

saved_print("PASS")
