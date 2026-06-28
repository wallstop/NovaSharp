-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:892
-- @test: MathModuleTUnitTests.RandomErrorsOnInfinityLua53Plus
-- Test targets Lua 5.3+
return math.random(1/0)
