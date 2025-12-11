-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:337
-- @test: MathModuleTUnitTests.FloorReturnsIntegerForPositiveFloat
return math.floor(3.7)
