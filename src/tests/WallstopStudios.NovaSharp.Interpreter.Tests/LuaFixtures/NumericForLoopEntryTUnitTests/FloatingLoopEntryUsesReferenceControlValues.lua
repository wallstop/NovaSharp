-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopEntryTUnitTests.cs:11
-- @test: NumericForLoopEntryTUnitTests.FloatingLoopEntryUsesReferenceControlValues

local legacy = _VERSION == 'Lua 5.1' or _VERSION == 'Lua 5.2' or _VERSION == 'Lua 5.3'
local first, second, count = nil, nil, 0
for value = -1e-16, 1, 1.0 do
    count = count + 1
    if count == 1 then first = value else second = value end
end
local cancellation = (first == (legacy and 0 or -1e-16))
    and (second == (legacy and 1 or 0.9999999999999999)) and count == 2
local reciprocal
for value = -0.0, 0.0, 1.0 do reciprocal = 1 / value end
local signed_zero = reciprocal == (legacy and math.huge or -math.huge)
local control = {}
for value = 1.0, 2.0, math.huge do control[1] = value; break end
for value = 2.0, 1.0, -math.huge do control[2] = value; break end
for value = 2.0, 1.0, 0 / 0 do control[3] = value; break end
local nonfinite = legacy and next(control) == nil
    or not legacy and control[1] == 1 and control[2] == 2 and control[3] == 2
assert(cancellation, 'incorrect first or second floating control value')
assert(signed_zero, 'incorrect sign of the first zero control value')
assert(nonfinite, 'incorrect entry with a non-finite step')
