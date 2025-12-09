-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaRandomParityTUnitTests.cs:313
-- @test: LuaRandomParityTUnitTests.MathRandomSeedLua54ReturnsSeeds
-- @compat-notes: Test targets Lua 5.2+
return math.randomseed(42)
