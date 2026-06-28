-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:2458
-- @test: MathModuleTUnitTests.CeilNegativeEdgeCases
-- NovaSharp: unresolved C# interpolation placeholder
return math.ceil({input})
