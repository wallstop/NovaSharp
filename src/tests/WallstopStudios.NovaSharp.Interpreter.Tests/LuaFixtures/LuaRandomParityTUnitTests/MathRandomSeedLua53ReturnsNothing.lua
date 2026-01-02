-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaRandomParityTUnitTests.cs:337
-- @test: LuaRandomParityTUnitTests.MathRandomSeedLua53ReturnsNothing
-- @compat-notes: Test targets Lua 5.2+
return math.randomseed(42)
