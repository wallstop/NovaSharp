-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:119
-- @test: LoadModuleVersionParityTUnitTests.LoadAcceptsStringArgumentInLua52Plus
-- @compat-notes: In Lua 5.2+, load() accepts both strings and reader functions

-- Test: load() should accept string arguments in Lua 5.2+
-- Reference: Lua 5.2+ Reference Manual - load (chunk [, chunkname [, mode [, env]]])

local fn, err = load("return 42")
assert(fn ~= nil, "load should accept string in Lua 5.2+, got error: " .. tostring(err))
assert(type(fn) == "function", "load should return a function, got " .. type(fn))

local result = fn()
assert(result == 42, "expected 42, got " .. tostring(result))

print("PASS: load accepts string argument in Lua 5.2+")
return result
