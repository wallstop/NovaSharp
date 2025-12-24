-- @lua-versions: 5.1, 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaRandomParityTUnitTests.cs:402
-- @test: LuaRandomParityTUnitTests.SameSeededLua51ScriptsProduceSameRandomSequence
-- @compat-notes: Test targets Lua 5.1
return math.random(), math.random(), math.random()
