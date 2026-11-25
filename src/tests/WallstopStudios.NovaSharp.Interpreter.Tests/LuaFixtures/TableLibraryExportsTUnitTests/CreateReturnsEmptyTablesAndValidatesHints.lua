-- @lua-versions: 5.5+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableLibraryExportsTUnitTests.cs:35
-- @test: TableLibraryExportsTUnitTests.CreateReturnsEmptyTablesAndValidatesHints
local empty = table.create(0)
local hinted = table.create('0x4', 2.0)
assert(type(empty) == 'table' and type(hinted) == 'table')
assert(empty ~= hinted and #empty == 0 and #hinted == 0)
assert(next(empty) == nil and next(hinted) == nil)
assert(getmetatable(hinted) == nil)
assert(select('#', table.create(0, nil, 'ignored')) == 1)
for i = 1, 5 do hinted[i] = i * 2 end
hinted.a, hinted.b, hinted.c = 1, 2, 3
assert(#hinted == 5 and hinted[5] == 10 and hinted.c == 3)
assert(next(empty) == nil)

local sequence = table.create(16)
sequence.tag = true
for i = 1, 16 do sequence[i] = i * 2 end
assert(#sequence == 16 and sequence[16] == 32 and sequence.tag)

local function check_error(arg, expected, ...)
    local ok, err = pcall(table.create, ...)
    assert(not ok and string.find(err, 'bad argument #' .. arg, 1, true)
        and string.find(err, expected, 1, true), tostring(err))
end
check_error(1, 'number expected', nil)
check_error(1, 'number expected')
check_error(1, 'number expected', '4' .. string.char(0))
check_error(1, 'number expected', false)
check_error(2, 'number expected', 2147483648, true)
check_error(1, 'number has no integer representation', 1.25)
check_error(2, 'number has no integer representation', 0, math.huge)
check_error(2, 'number has no integer representation', 0, 0/0)
check_error(1, 'out of range', -1)
check_error(2, 'out of range', 0, -1)
check_error(1, 'out of range', 2147483648)
check_error(2, 'out of range', 0, math.maxinteger)
local ok, err = pcall(table.create, 0, 2147483647)
assert(not ok and string.find(err, 'table overflow', 1, true), tostring(err))
