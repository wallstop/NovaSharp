-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/ClosureTUnitTests.cs:17
-- @test: ClosureTUnitTests.GetUpValuesTypeReturnsEnvironmentWhenOnlyEnvIsCaptured
return function(a) return a end
