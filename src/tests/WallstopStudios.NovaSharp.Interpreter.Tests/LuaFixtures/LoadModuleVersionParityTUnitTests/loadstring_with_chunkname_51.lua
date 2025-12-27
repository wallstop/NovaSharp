-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:206
-- @test: LoadModuleVersionParityTUnitTests.LoadstringWithChunknameInLua51
-- @compat-notes: loadstring accepts optional chunkname parameter in Lua 5.1

-- Test: loadstring with chunkname parameter in Lua 5.1
-- Reference: Lua 5.1 Reference Manual §5.1 - loadstring (string [, chunkname])

local fn, err = loadstring("return 'hello'", "mychunk")
assert(fn ~= nil, "loadstring should accept chunkname, got error: " .. tostring(err))
assert(type(fn) == "function", "loadstring should return a function")

local result = fn()
assert(result == "hello", "expected 'hello', got " .. tostring(result))

print("PASS: loadstring with chunkname works in Lua 5.1")
return result
