-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathIntegerFunctionsTUnitTests.cs:296
-- @test: MathIntegerFunctionsTUnitTests.MathModfIntegerBoundaries
-- NovaSharp: unresolved C# interpolation placeholder
return math.modf({expression})
