-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:1715
-- @test: MathModuleTUnitTests.RandomSucceedsWithPositiveInfinitySecondArgLua52Only
-- Test targets Lua 5.2+
return math.random(1, 1/0)
