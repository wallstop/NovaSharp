-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:175
-- @test: LoadModuleVersionParityTUnitTests.LoadEnvParameterWorksInLua52Plus
-- @compat-notes: The env parameter was added in Lua 5.2

-- Test: load() env parameter works in Lua 5.2+
-- Reference: Lua 5.2+ Reference Manual - load (chunk [, chunkname [, mode [, env]]])

local customEnv = { value = 100, print = print }
local fn, err = load("return value", "test", "t", customEnv)
assert(fn ~= nil, "load should accept env parameter, got error: " .. tostring(err))

local result = fn()
assert(result == 100, "expected 100 from custom env, got " .. tostring(result))

print("PASS: load env parameter works in Lua 5.2+")
return result
