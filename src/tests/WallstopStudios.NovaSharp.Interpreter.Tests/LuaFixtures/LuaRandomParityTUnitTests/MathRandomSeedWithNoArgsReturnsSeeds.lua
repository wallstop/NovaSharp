-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaRandomParityTUnitTests.cs:301
-- @test: LuaRandomParityTUnitTests.MathRandomSeedWithNoArgsReturnsSeeds
-- @compat-notes: Test targets Lua 5.3+
return math.randomseed()
