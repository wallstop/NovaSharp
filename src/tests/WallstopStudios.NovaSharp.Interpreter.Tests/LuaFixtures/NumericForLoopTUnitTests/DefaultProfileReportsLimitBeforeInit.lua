-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:645
-- @test: NumericForLoopTUnitTests.DefaultProfileReportsLimitBeforeInit
for i = {}, {}, {} do end
