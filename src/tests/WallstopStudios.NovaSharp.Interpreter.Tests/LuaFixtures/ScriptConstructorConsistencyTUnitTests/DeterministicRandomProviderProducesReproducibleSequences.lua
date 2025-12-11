-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\ScriptConstructorConsistencyTUnitTests.cs:236
-- @test: ScriptConstructorConsistencyTUnitTests.DeterministicRandomProviderProducesReproducibleSequences
return math.random(), math.random(), math.random()
