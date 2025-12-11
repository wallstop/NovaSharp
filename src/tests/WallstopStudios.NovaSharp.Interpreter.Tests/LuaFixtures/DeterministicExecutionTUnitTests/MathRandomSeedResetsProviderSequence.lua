-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Sandbox\DeterministicExecutionTUnitTests.cs:291
-- @test: DeterministicExecutionTUnitTests.MathRandomSeedResetsProviderSequence
-- @compat-notes: Lua 5.3+: bitwise operators
r1 = math.random(); r2 = math.random(); r3 = math.random()
