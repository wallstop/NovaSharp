-- @lua-versions: 5.3+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableLibraryIndexedAccessTUnitTests.cs:43
-- @test: TableLibraryIndexedAccessTUnitTests.UnpackChecksAvailableStackBeforeIndexing
local reads = 0
local proxy = setmetatable({}, {
    __index = function(_, key)
        reads = reads + 1
        return key
    end
})

assert(table.unpack(proxy, 1, 1) == 1 and reads == 1)
local ok, result = pcall(table.unpack, proxy, 1, 64)
assert(not ok and result:find('too many results to unpack', 1, true))
assert(reads == 1, 'an oversized unpack must fail before accessing any element')
