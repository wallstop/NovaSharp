-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableLibraryExportsTUnitTests.cs:26
-- @test: TableLibraryExportsTUnitTests.LegacyHelpersMatchLua51
local t = setmetatable({11, 22, 33, n = 99}, {
    __len = function() error('length must be raw') end,
    __index = function() error('index must be raw') end
})
assert(table.getn(t) == 3)
assert(table.getn({}) == 0)
local visited = {}
local function visit(i, value)
    assert(i == #visited + 1)
    visited[i] = value or 'nil'
    if i == 1 then
        t[2] = nil
        t[4] = 44
        return nil, 'ignored'
    elseif i == 3 then
        return false, 'ignored'
    end
end
local function check_stop(...)
    assert(select('#', ...) == 1 and ... == false)
end
check_stop(table.foreachi(t, visit))
assert(table.concat(visited, ',') == '11,nil,33')
assert(select('#', table.foreachi({1}, function() end)) == 0)
assert(table.foreachi({1}, type) == 'number')

local hash = {red = 1, green = 2, blue = 3}
local sum = 0
assert(select('#', table.foreach(hash, function(k, value)
    assert(hash[k] == value)
    sum = sum + value
    hash[k] = nil
end)) == 0)
assert(sum == 6 and next(hash) == nil)
check_stop(table.foreach({only = 1}, function() return false, true end))
assert(select('#', table.foreach({}, function() error('unexpected call') end)) == 0)
local function protected_key(value)
    assert(value == 'x')
end
assert(table.foreach({[protected_key] = 'x'}, pcall) == true)
check_stop(table.foreachi({'x'}, pcall))

local function check_error(expected, callback, ...)
    local ok, err = pcall(callback, ...)
    assert(not ok and string.find(err, expected, 1, true), tostring(err))
end
check_error('table expected', table.getn, 42)
check_error('table expected', table.setn, nil)
check_error("'setn' is obsolete", table.setn, {})
check_error("'setn' is obsolete", table.setn, {}, {})
check_error('function expected', table.foreachi, {}, setmetatable({}, {__call = type}))
check_error('function expected', table.foreach, {}, nil)
check_error('callback failed', table.foreach, {only = true}, function() error('callback failed') end)

local running = coroutine.create(function()
    table.foreachi({1}, coroutine.yield)
end)
local resumed, message = coroutine.resume(running)
assert(not resumed and string.find(message, 'yield across', 1, true), tostring(message))
