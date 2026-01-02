-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaRandomParityTUnitTests.cs:365
-- @test: LuaRandomParityTUnitTests.SameSeededScriptsProduceSameRandomSequence
-- @compat-notes: Test targets Lua 5.4+
return math.random(), math.random(), math.random()
