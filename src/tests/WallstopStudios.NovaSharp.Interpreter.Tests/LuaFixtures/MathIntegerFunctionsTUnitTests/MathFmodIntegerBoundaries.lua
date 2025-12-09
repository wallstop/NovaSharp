-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathIntegerFunctionsTUnitTests.cs:267
-- @test: MathIntegerFunctionsTUnitTests.MathFmodIntegerBoundaries
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return math.fmod({x}, {y})
