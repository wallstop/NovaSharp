-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/VmCorrectnessRegressionTUnitTests.cs:306
-- @test: TestUserDataObject2.DebugUpValueJoinIsAvailable
return type(debug.upvaluejoin)
