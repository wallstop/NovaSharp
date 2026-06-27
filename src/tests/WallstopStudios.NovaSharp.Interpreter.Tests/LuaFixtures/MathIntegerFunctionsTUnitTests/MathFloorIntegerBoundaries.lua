-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathIntegerFunctionsTUnitTests.cs:212
-- @test: MathIntegerFunctionsTUnitTests.MathFloorIntegerBoundaries
-- NovaSharp: unresolved C# interpolation placeholder
return math.floor({expression})
