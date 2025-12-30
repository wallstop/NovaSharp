-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:2404
-- @test: MathModuleTUnitTests.FloorNegativeEdgeCases
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return math.floor({input})
