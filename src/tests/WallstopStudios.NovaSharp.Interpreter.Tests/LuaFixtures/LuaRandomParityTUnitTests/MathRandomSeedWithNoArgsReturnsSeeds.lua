-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaRandomParityTUnitTests.cs:301
-- @test: LuaRandomParityTUnitTests.MathRandomSeedWithNoArgsReturnsSeeds
-- @compat-notes: Test targets Lua 5.3+
return math.randomseed()
