-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableLibraryExportsTUnitTests.cs:17
-- @test: TableLibraryExportsTUnitTests.ExportsMatchReferenceBuild
local expected = {
    ['Lua 5.1'] = 'concat,foreach,foreachi,getn,insert,maxn,remove,setn,sort',
    ['Lua 5.2'] = 'concat,insert,pack,remove,sort,unpack',
    ['Lua 5.3'] = 'concat,insert,move,pack,remove,sort,unpack',
    ['Lua 5.4'] = 'concat,insert,move,pack,remove,sort,unpack',
    ['Lua 5.5'] = 'concat,create,insert,move,pack,remove,sort,unpack'
}
local names = {}
for key, callback in pairs(table) do
    assert(type(callback) == 'function')
    names[#names + 1] = key
end
table.sort(names)
local reference = expected[_VERSION]
if _VERSION == 'Lua 5.1' then
    assert(type(unpack) == 'function' and unpack({'one'}) == 'one')
elseif _VERSION == 'Lua 5.2' then
    -- Stock make enables LUA_COMPAT_ALL. The Windows reference build omits it.
    if table.maxn then
        reference = 'concat,insert,maxn,pack,remove,sort,unpack'
    end
    assert(unpack == nil or rawequal(unpack, table.unpack))
else
    assert(unpack == nil)
end
assert(Unpack == nil)
assert(table.concat(names, ',') == reference, table.concat(names, ','))
