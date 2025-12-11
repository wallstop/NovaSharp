-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Compatibility\IntegerBoundaryTUnitTests.cs:265
-- @test: IntegerBoundaryTUnitTests.MathTointegerBoundaryValues
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return math.tointeger({expression})
