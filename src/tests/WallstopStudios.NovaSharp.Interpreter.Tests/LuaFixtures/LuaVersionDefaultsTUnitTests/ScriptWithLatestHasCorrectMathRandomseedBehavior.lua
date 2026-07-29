-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaVersionDefaultsTUnitTests.cs:196
-- @test: LuaVersionDefaultsTUnitTests.ScriptWithLatestHasCorrectMathRandomseedBehavior
-- Test targets Lua 5.4+
return math.randomseed(12345)
