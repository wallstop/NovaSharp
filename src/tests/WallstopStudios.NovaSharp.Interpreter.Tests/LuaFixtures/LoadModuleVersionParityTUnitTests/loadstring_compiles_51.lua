-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:66
-- @test: LoadModuleVersionParityTUnitTests.LoadstringCompilesAndExecutesInLua51
-- @compat-notes: loadstring compiles a string and returns a function in Lua 5.1

-- Test: loadstring compiles code and returns executable function in Lua 5.1
-- Reference: Lua 5.1 Reference Manual §5.1 - loadstring

local fn, err = loadstring("return 42")
assert(fn ~= nil, "loadstring should return a function, got error: " .. tostring(err))
assert(type(fn) == "function", "loadstring should return a function, got " .. type(fn))

local result = fn()
assert(result == 42, "expected 42, got " .. tostring(result))

print("PASS: loadstring compiles and executes in Lua 5.1")
return result
