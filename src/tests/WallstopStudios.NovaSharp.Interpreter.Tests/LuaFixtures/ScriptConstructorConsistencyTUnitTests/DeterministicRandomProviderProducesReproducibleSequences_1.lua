-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/ScriptConstructorConsistencyTUnitTests.cs:321
-- @test: ScriptConstructorConsistencyTUnitTests.DeterministicRandomProviderProducesReproducibleSequences
-- @compat-notes: Test targets Lua 5.3+
return math.random(), math.random(), math.random()
