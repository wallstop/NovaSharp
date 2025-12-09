-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Sandbox\DeterministicExecutionTUnitTests.cs:297
-- @test: DeterministicExecutionTUnitTests.MathRandomSeedResetsProviderSequence
-- @compat-notes: Lua 5.3+: bitwise operators
math.randomseed(42); r1b = math.random(); r2b = math.random(); r3b = math.random()
