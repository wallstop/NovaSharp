-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1307
-- @test: SimpleTUnitTests.SelectHashCountsExpandedNilReturnValues
local function values()
    return 'a', nil, 'c'
end

local count, first, second, third = select('#', values()), values()

assert(count == 3, 'expanded count')
assert(first == 'a', 'first value')
assert(second == nil, 'second value')
assert(third == 'c', 'third value')

return count, first, second, third
