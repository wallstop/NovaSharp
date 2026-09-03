-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:188
-- @test: NumericForLoopTUnitTests.IntegerBoundaryLoopsNeverWrapTheControlVariable
-- Scoped to 5.4+ because reference Lua 5.3 loops forever on ranges that reach the
-- integer extremes; NovaSharp follows the corrected 5.4 counter semantics there.
local mi = math.mininteger
local ma = math.maxinteger

local function collect(init, limit, step)
    local t = {}
    for i = init, limit, step do
        t[#t + 1] = i
    end
    return table.concat(t, ",")
end

print(collect(ma - 2, ma, 1))
print(collect(ma - 2, ma, 2))
print(collect(mi + 2, mi, -1))
print(collect(mi, mi + 3, 1))
print(collect(0, ma, ma))
print(collect(ma, 0, -ma))
print(collect(ma, mi, -ma))

-- A float limit beyond the integer range clamps to the boundary the loop walks toward.
print(collect(0, 2e63, ma))
