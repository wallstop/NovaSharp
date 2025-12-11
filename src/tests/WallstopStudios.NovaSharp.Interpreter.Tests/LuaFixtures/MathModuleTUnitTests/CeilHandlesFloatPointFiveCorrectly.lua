-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:496
-- @test: MathModuleTUnitTests.CeilHandlesFloatPointFiveCorrectly
return math.ceil(0.5)
