-- Test: getfenv error cases
-- Expected: success
-- Description: Tests getfenv error handling

-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs
-- @test: SetFenvGetFenvTUnitTests.getfenv_errors_51
local saved_print = print

-- Test: getfenv with invalid level throws error
local ok1, err1 = pcall(function() getfenv(100) end)
assert(not ok1, "getfenv(100) should throw error")
assert(err1:find("invalid level"), "Error should mention 'invalid level'")

-- Test: getfenv with negative level throws error
local ok2, err2 = pcall(function() getfenv(-1) end)
assert(not ok2, "getfenv(-1) should throw error")
assert(err2:find("non-negative") or err2:find("negative"), "Error should mention negative level")

-- Test: getfenv with string throws error
local ok3, err3 = pcall(function() getfenv("string") end)
assert(not ok3, "getfenv(string) should throw error")
assert(err3:find("number expected"), "Error should say 'number expected'")

-- Test: getfenv with boolean throws error
local ok4, err4 = pcall(function() getfenv(true) end)
assert(not ok4, "getfenv(true) should throw error")
assert(err4:find("number expected"), "Error should say 'number expected'")

-- Test: getfenv with table throws error
local ok5, err5 = pcall(function() getfenv({}) end)
assert(not ok5, "getfenv({}) should throw error")
assert(err5:find("number expected"), "Error should say 'number expected'")

saved_print("PASS")
