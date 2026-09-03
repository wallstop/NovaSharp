-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:56
-- @test: NumericForLoopTUnitTests.ZeroCrossingLoopsIterateEveryValue
local function collect(init, limit, step)
    local t = {}
    if step then
        for i = init, limit, step do
            t[#t + 1] = i
        end
    else
        for i = init, limit do
            t[#t + 1] = i
        end
    end
    return table.concat(t, ",")
end

print(collect(-2, 2))
print(collect(2, -2, -1))
print(collect(-3, 3, 2))
print(collect(3, -3, -2))
print(collect(-1, 1))
print(collect(1, -1, -1))
print(collect(-10, 10, 5))
print(collect(10, -10, -5))
print(collect(1, 3))
print(collect(5, 3))
print(collect(1, 5, 2))
print(collect(1, 1))
print(collect(-3, -1))

-- The control variable is scoped to the loop body in every version.
for i = -2, 2 do end
print(i)
