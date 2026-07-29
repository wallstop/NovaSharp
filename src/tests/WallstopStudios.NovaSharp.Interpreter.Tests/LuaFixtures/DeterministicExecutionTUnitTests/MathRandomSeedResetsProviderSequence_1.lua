-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/DeterministicExecutionTUnitTests.cs:330
-- @test: DeterministicExecutionTUnitTests.MathRandomSeedResetsProviderSequence
math.randomseed(42); r1b = math.random(); r2b = math.random(); r3b = math.random()
