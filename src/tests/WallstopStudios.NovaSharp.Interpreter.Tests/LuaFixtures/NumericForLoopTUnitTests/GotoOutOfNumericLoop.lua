-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:565
-- @test: NumericForLoopTUnitTests.GotoOutOfNumericLoopDoesNotLeakControlSlots
-- Each backward goto out of the loop must pop the control triple; leaking it once grew
-- the value stack until it overflowed.
local n = 0
::top::
for i = 1, 2 do
    if n < 100000 then
        n = n + 1
        goto top
    end
end
print(n)
