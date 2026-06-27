-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathModuleTUnitTests.cs:2539
-- @test: MathModuleTUnitTests.FmodSignedDividendEdgeCases
-- NovaSharp: unresolved C# interpolation placeholder
return math.fmod({dividend}, {divisor})
