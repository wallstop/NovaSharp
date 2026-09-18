-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:644
-- @test: NumericForLoopTUnitTests.InvalidInitReportedBeforeInvalidLimit
for i = {}, {}, {} do end
