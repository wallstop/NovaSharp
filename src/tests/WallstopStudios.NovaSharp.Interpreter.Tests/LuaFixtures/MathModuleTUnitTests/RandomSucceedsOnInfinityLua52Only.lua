-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:822
-- @test: MathModuleTUnitTests.RandomSucceedsOnInfinityLua52Only
-- @compat-notes: Test targets Lua 5.1
return math.random(1/0)
