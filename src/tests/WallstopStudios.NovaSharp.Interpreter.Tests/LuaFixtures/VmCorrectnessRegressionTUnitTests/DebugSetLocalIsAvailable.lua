-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/VmCorrectnessRegressionTUnitTests.cs:151
-- @test: VmCorrectnessRegressionTUnitTests.DebugSetLocalIsAvailable
return type(debug.setlocal)
