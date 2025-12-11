-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Compatibility\IntegerBoundaryTUnitTests.cs:317
-- @test: IntegerBoundaryTUnitTests.MathTointegerStringArguments
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return math.tointeger({expression})
