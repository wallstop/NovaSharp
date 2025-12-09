-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaVersionDefaultsTUnitTests.cs:203
-- @test: LuaVersionDefaultsTUnitTests.ScriptWithLatestHasCorrectMathRandomseedBehavior
-- @compat-notes: Test targets Lua 5.4+
return math.randomseed(12345)
