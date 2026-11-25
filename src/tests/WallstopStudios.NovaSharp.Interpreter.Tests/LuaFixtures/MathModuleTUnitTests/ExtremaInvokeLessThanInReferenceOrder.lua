-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:2657
-- @test: MathModuleTUnitTests.ExtremaInvokeLessThanInReferenceOrder
local log = {}
local mt = {
    __lt = function(left, right)
        log[#log + 1] = left.id .. right.id
        if left.rank < right.rank then return 0, false end
        return nil, true
    end
}
local a = setmetatable({id = 'a', rank = 2}, mt)
local b = {id = 'b', rank = 1}
local c = setmetatable({id = 'c', rank = 3}, mt)
local tied = setmetatable({id = 't', rank = 3}, mt)
assert(rawequal(math.min(a, b, c), b))
assert(table.concat(log, ',') == 'ba,cb')
log = {}
assert(rawequal(math.max(a, b, c, tied), c))
assert(table.concat(log, ',') == 'ab,ac,ct')

local callable = setmetatable({}, {__call = function(self, left, right)
    assert(self ~= left and self ~= right)
    return left.rank < right.rank
end})
local lowest = setmetatable({rank = 0}, {__lt = callable})
assert(rawequal(math.min(c, b, lowest), lowest))

local expanded = {}
for rank = 1, 10 do
    expanded[rank] = setmetatable({id = rank, rank = rank}, mt)
end
log = {}
assert(rawequal(math.max(table.unpack(expanded)), expanded[10]))
assert(#log == 9)

local co
local guarded = setmetatable({}, {__lt = function()
    assert(coroutine.running() == co and math.max(1, 2) == 2)
    coroutine.yield('unexpected')
end})
co = coroutine.create(function()
    local ok, err = pcall(math.min, a, guarded)
    assert(not ok and err:find('attempt to yield', 1, true))
    return 'continued'
end)
local ok, value = coroutine.resume(co)
assert(ok and value == 'continued' and coroutine.status(co) == 'dead')
local failed = setmetatable({}, {__lt = function() error('comparison failed', 0) end})
local success, err = pcall(math.min, a, failed)
assert(not success and err:find('comparison failed', 1, true))
return true
