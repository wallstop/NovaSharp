-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:376
-- @test: MathModuleTUnitTests.FrexpWithZeroReturnsZeroMantissaAndExponent
-- Test targets Lua 5.3+
return math.frexp(0)
