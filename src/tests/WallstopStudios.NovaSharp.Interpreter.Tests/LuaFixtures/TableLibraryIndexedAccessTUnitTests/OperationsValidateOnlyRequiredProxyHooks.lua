-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableLibraryIndexedAccessTUnitTests.cs:23
-- @test: TableLibraryIndexedAccessTUnitTests.OperationsValidateOnlyRequiredProxyHooks
local modern = _VERSION ~= 'Lua 5.1' and _VERSION ~= 'Lua 5.2'
local unpack_values = table.unpack or unpack
local source, destination = false, 1
local backing = {'b', 'a'}
local written = {}
local source_meta = {__index = backing}
local old_source = debug.getmetatable(source)
local old_destination = debug.getmetatable(destination)
debug.setmetatable(source, source_meta)
debug.setmetatable(destination, {__newindex = written})

local ok, result = pcall(unpack_values, source, 1, 1)
assert(ok == modern and (not modern or result == 'b'), 'unpack does not require length with explicit end')
assert(not pcall(unpack_values, source), 'unpack default end requires length')
ok, result = pcall(function() return select('#', unpack_values(nil, 2, 1)) end)
assert(ok == modern and (not modern or result == 0), 'unpack empty range avoids indexing')

assert(not pcall(table.concat, source, ',', 1, 2), 'concat requires length even with explicit end')
local length_calls = 0
source_meta.__len = function()
    length_calls = length_calls + 1
    return 2
end
ok, result = pcall(table.concat, source, ',', 1, 2)
assert(ok == modern and (not modern or result == 'b,a'), 'concat requires only read and length')
assert(not pcall(table.remove, source, 1) and not pcall(table.sort, source), 'remove and sort require write')

if modern then
    assert(length_calls == 1, 'failed preflight did not evaluate length')
    assert(table.move(source, 1, 2, 3, destination) == destination)
    assert(written[3] == 'b' and written[4] == 'a' and length_calls == 1, 'move only requires read and write')
    assert(not pcall(table.move, destination, 1, 0, 1, source), 'move validates both sides of an empty range')
    source_meta.__newindex = backing
    assert(select('#', table.sort(source)) == 0)
    assert(backing[1] == 'a' and backing[2] == 'b')
    assert(table.remove(source, 1) == 'a' and backing[1] == 'b' and backing[2] == nil)

    source_meta.__index = nil
    setmetatable(source_meta, {__index = {__index = backing}})
    assert(not pcall(table.concat, source, ',', 1, 1), 'preflight reads raw metatable fields')
    source_meta.__index = false
    assert(table.concat(source, ',', 2, 1) == '', 'false is a present raw hook')

    source_meta.__len = function()
        debug.setmetatable(source, {__index = backing})
        return 1
    end
    assert(table.concat(source) == 'b', 'length hook can replace the metatable')
end

debug.setmetatable(source, old_source)
debug.setmetatable(destination, old_destination)
