-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:323
-- @test: NumericForLoopTUnitTests.ZeroStepErrorsFromLua54
for i = 1, 10, 0 do
    print("unreachable")
end
