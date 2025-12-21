-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/VmCorrectnessRegressionTUnitTests.cs:103
-- @test: VmCorrectnessRegressionTUnitTests.SetUpValueThrowsForInvalidIndex
return function() return 1 end
