-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:912
-- @test: MathModuleTUnitTests.RandomErrorsOnSecondNonIntegerArgLua53Plus
-- Test targets Lua 5.3+
return math.random(1, 2.5)
