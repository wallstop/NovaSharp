-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs
-- @test: ScriptCallTUnitTests.DynValueArgumentsBindToWritableLocalSlots

local scalar = 1
local shared = { field = 1 }

local function mutate(a, ...)
    local first, second = ...
    a = 11
    first = 22
    second.field = 33
    return a, first, second.field, scalar, shared.field, select("#", ...)
end

local a, first, field, originalScalar, sharedField, count = mutate(scalar, 2, shared)
assert(a == 11)
assert(first == 22)
assert(field == 33)
assert(originalScalar == 1)
assert(sharedField == 33)
assert(count == 2)

local function capture(...)
    local values = { ... }
    return values[1], values[2], #values
end

local x = 5
local y = 6
local one, two, total = capture(x, y)
x = 50
y = 60

assert(one == 5)
assert(two == 6)
assert(total == 2)

print("PASS")
