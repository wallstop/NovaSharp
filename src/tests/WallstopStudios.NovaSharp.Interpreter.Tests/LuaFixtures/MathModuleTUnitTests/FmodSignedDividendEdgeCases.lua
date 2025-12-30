-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:2515
-- @test: MathModuleTUnitTests.FmodSignedDividendEdgeCases
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return math.fmod({dividend}, {divisor})
