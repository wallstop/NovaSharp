-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:280
-- @test: MathModuleTUnitTests.FrexpWithSubnormalNumberHandlesExponentCorrectly
return math.frexp(subnormal)
