-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaRandomParityTUnitTests.cs:288
-- @test: LuaRandomParityTUnitTests.MathRandomSeedReturnsSeeds
-- @compat-notes: Test targets Lua 5.4+
return math.randomseed(42)
