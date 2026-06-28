-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:168
-- @test: MathModuleTUnitTests.PowWithLargeExponentReturnsInfinity
-- Test targets Lua 5.4+
return math.pow(10, 309)
