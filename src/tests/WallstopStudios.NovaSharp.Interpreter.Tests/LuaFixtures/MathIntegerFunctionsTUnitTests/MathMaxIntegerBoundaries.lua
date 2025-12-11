-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathIntegerFunctionsTUnitTests.cs:342
-- @test: MathIntegerFunctionsTUnitTests.MathMaxIntegerBoundaries
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return math.max({args})
