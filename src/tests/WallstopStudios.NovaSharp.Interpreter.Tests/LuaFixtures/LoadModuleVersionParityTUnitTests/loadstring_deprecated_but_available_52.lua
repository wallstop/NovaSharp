-- @lua-versions: 5.2
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:38
-- @test: LoadModuleVersionParityTUnitTests.LoadstringIsDeprecatedButAvailableInLua52
-- @compat-notes: Platform-specific: Windows Lua binary built without LUA_COMPAT_LOADSTRING. NovaSharp provides loadstring for Lua 5.2 compatibility

-- Test: loadstring should be available (deprecated) in Lua 5.2
-- Reference: Lua 5.2 Reference Manual Section 8.2 - "Function loadstring is deprecated"

assert(type(loadstring) == "function", "loadstring should be a function in Lua 5.2")

-- Test that loadstring still works
local f, err = loadstring("return 42")
assert(f ~= nil, "loadstring should compile code: " .. tostring(err))
local result = f()
assert(result == 42, "loadstring-compiled code should execute correctly")

print("PASS: loadstring is deprecated but available in Lua 5.2")
return type(loadstring)