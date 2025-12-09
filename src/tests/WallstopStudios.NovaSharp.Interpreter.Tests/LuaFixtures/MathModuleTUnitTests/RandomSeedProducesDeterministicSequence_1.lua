-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:124
-- @test: MathModuleTUnitTests.RandomSeedProducesDeterministicSequence
math.randomseed(1337)
                return math.random(1, 100), math.random(1, 100), math.random()
