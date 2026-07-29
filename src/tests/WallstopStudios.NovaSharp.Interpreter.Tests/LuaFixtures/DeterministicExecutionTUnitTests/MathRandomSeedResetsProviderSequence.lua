-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/DeterministicExecutionTUnitTests.cs:324
-- @test: DeterministicExecutionTUnitTests.MathRandomSeedResetsProviderSequence
r1 = math.random(); r2 = math.random(); r3 = math.random()
