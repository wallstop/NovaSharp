-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:666
-- @test: NumericForLoopTUnitTests.InvalidInitReportedBeforeInvalidStep
for i = {}, 10, {} do end
