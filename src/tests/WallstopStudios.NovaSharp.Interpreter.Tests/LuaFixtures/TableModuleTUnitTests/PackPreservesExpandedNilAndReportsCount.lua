-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:35
-- @test: TableModuleTUnitTests.PackPreservesExpandedNilAndReportsCount
local function values()
    return 'a', nil, 'c'
end

local packed = table.pack('head', values())
assert(packed.n == 4, 'packed count')
assert(packed[1] == 'head', 'packed head')
assert(packed[2] == 'a', 'packed expanded first')
assert(packed[3] == nil, 'packed expanded nil')
assert(packed[4] == 'c', 'packed expanded third')

return packed.n, packed[1], packed[2], packed[3], packed[4]
