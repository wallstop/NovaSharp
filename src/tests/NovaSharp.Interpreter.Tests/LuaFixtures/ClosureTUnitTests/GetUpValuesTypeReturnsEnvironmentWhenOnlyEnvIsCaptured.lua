-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ClosureTUnitTests.cs:15
-- @test: ClosureTUnitTests.GetUpValuesTypeReturnsEnvironmentWhenOnlyEnvIsCaptured
return function(a) return a end
