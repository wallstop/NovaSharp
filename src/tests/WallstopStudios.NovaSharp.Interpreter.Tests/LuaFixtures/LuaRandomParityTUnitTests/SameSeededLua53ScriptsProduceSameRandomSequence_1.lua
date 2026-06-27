-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaRandomParityTUnitTests.cs:430
-- @test: LuaRandomParityTUnitTests.SameSeededLua53ScriptsProduceSameRandomSequence
-- Test targets Lua 5.1
return math.random(), math.random(), math.random()
