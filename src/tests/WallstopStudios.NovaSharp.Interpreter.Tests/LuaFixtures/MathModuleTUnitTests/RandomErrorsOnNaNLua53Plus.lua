-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:798
-- @test: MathModuleTUnitTests.RandomErrorsOnNaNLua53Plus
-- Test targets Lua 5.1
return math.random(0/0)
