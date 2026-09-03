-- @lua-versions: 5.1-5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:349
-- @test: NumericForLoopTUnitTests.MutatingControlVariableDoesNotChangeIterationCount
-- Lua 5.5 makes the numeric for-loop control variable const, so this fixture stops at 5.4.
local mutated = {}
for i = -2, 2 do
    mutated[#mutated + 1] = i
    i = i + 100
end
print(table.concat(mutated, ","))
