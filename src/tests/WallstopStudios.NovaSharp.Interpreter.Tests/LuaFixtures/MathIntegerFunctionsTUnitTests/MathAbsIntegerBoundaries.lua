-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathIntegerFunctionsTUnitTests.cs:41
-- @test: MathIntegerFunctionsTUnitTests.MathAbsIntegerBoundaries
-- NovaSharp: unresolved C# interpolation placeholder
return math.abs({expression})
