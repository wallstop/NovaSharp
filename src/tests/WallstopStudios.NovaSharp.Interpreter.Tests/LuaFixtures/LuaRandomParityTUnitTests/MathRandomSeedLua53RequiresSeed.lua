-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaRandomParityTUnitTests.cs:363
-- @test: LuaRandomParityTUnitTests.MathRandomSeedLua53RequiresSeed
-- @compat-notes: Test targets Lua 5.2+
math.randomseed()
