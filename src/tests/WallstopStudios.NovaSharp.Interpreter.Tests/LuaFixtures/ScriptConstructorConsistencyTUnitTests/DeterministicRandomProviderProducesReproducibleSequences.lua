-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/ScriptConstructorConsistencyTUnitTests.cs:320
-- @test: ScriptConstructorConsistencyTUnitTests.DeterministicRandomProviderProducesReproducibleSequences
-- @compat-notes: Test targets Lua 5.1
return math.random(), math.random(), math.random()
