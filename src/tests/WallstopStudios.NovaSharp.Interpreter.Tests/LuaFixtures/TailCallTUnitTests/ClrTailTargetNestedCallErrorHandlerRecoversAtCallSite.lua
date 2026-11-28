-- @lua-versions: all
-- @novasharp-only: true
-- @expects-error: false
-- Uses injected variables: bridge, nonTailBridge
local direct = bridge()
assert(direct == 'recovered', 'direct nested recovery: ' .. tostring(direct))

local left, middle, right = 'left', bridge(), 'right'
assert(left == 'left', 'left caller argument: ' .. tostring(left))
assert(middle == 'recovered', 'middle caller argument: ' .. tostring(middle))
assert(right == 'right', 'right caller argument: ' .. tostring(right))

local repeated = bridge() .. '|' .. bridge()
assert(repeated == 'recovered|recovered', 'repeated nested recovery: ' .. tostring(repeated))

local nonTail = nonTailBridge()
assert(nonTail == 'recovered', 'non-tail nested recovery: ' .. tostring(nonTail))

local mixed = nonTailBridge() .. '|' .. bridge()
assert(mixed == 'recovered|recovered', 'mixed nested recovery: ' .. tostring(mixed))

return table.concat({ direct, middle, nonTail, repeated, mixed }, ';')
