-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:151
-- @test: NumericForLoopTUnitTests.FloatLoopWithFloatInitIteratesFractionalValues
local function collect(init, limit, step)
    local t = {}
    for i = init, limit, step do
        t[#t + 1] = i
    end
    return table.concat(t, ",")
end

-- Float init with integer limit: comparison-driven in every reference version.
print(collect(1.5, 3, 1))
print(collect(3.5, 1, -1))

-- Fractional limits with integer init and step: reference Lua converts the limit
-- toward the loop direction, and every visited value stays integral.
print(collect(1, 3.5, 1))
print(collect(3, 1.5, -1))

-- Fractional steps: report the count only, because whole float values format
-- differently before and after Lua 5.3.
local half = 0
for i = 1, 3, 0.5 do
    half = half + 1
end
print(half)
