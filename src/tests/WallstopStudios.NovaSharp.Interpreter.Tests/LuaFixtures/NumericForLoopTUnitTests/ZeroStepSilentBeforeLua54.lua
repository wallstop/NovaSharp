-- @lua-versions: 5.1-5.3
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:302
-- @test: NumericForLoopTUnitTests.ZeroStepRunsZeroIterationsBeforeLua54
-- Reference Lua 5.1-5.3 run zero iterations for an ascending zero step. Their
-- descending form loops forever; NovaSharp terminates that direction as well.
local n = 0
for i = 1, 10, 0 do
    n = n + 1
end
for i = 1, 10, 0.0 do
    n = n + 1
end
print(n)
