-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5

local calls = ''
local function noValues()
    calls = calls .. 'n'
end
local function tailValues()
    calls = calls .. 't'
    return false, nil, 7
end

local single = noValues()
assert(single == nil and select('#', single) == 1, 'local from zero results')
local first, second, third, fourth, missing = noValues(), tailValues()
assert(first == nil and select('#', first) == 1, 'adjusted zero results')
assert(second == false and third == nil and fourth == 7, 'expanded results')
assert(missing == nil and select('#', missing) == 1, 'missing tuple slot')
assert(calls == 'nnt', 'assignment evaluation order')

local captured = true
local function update()
    captured = noValues()
end
update()
assert(captured == nil and select('#', captured) == 1, 'captured local')

assignment_probe_global = true
assignment_probe_global = noValues()
assert(assignment_probe_global == nil, 'global deletion')

local target = {existing = true}
target.existing = noValues()
assert(rawget(target, 'existing') == nil, 'table field deletion')
local assigned
setmetatable(target, {__newindex = function(_, key, value)
    assert(key == 'new_key' and value == nil and select('#', value) == 1, 'newindex value')
    assigned = true
end})
target.new_key = noValues()
assert(assigned, 'newindex call')

print('explicit nil')
