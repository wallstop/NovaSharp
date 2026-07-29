-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/VmCorrectnessRegressionTUnitTests.cs:162
-- @test: VmCorrectnessRegressionTUnitTests.DebugSetLocalIsAvailable
-- Compatibility notes: Test targets Lua 5.1
return type(debug.setlocal)
