-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:0
-- @test: LoadModuleVersionParityTUnitTests.DofileConvertsNumberToStringInLua52Plus
-- @compat-notes: dofile() converts number argument to string in Lua 5.2+ (number coercion)

-- Test: dofile() should convert number to string filename and return proper error
-- Reference: Lua 5.2+ allows number-to-string coercion

-- dofile with number argument - should fail with "cannot open" error (file not found)
local ok, err = pcall(function() return dofile(123) end)

assert(not ok, "dofile(123) should fail")
assert(err:find("cannot open"), "Error should say 'cannot open', got: " .. tostring(err))
assert(err:find("No such file"), "Error should mention 'No such file', got: " .. tostring(err))

print("PASS: dofile(123) properly converts to filename and returns file-not-found error")
return "PASS"
