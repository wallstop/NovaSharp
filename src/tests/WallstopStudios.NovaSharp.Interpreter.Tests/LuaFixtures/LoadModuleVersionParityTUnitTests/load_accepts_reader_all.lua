-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:147
-- @test: LoadModuleVersionParityTUnitTests.LoadAcceptsReaderFunctionInAllVersions
-- @compat-notes: load() with a reader function works in all Lua versions

-- Test: load() should accept reader functions in all Lua versions
-- Reference: Lua Reference Manual - load with reader function

-- Create a simple reader function that returns chunks
local chunks = { "local x = ", "10 + ", "32 return x" }
local index = 0
local function reader()
    index = index + 1
    return chunks[index]
end

local fn, err = load(reader)
assert(fn ~= nil, "load should accept reader function, got error: " .. tostring(err))
assert(type(fn) == "function", "load should return a function, got " .. type(fn))

local result = fn()
assert(result == 42, "expected 42, got " .. tostring(result))

print("PASS: load accepts reader function in all versions")
return result
