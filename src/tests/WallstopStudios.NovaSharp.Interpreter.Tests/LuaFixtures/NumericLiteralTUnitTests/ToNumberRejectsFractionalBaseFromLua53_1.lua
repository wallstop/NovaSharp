-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericLiteralTUnitTests.cs:380
-- @test: NumericLiteralTUnitTests.ToNumberRejectsFractionalBaseFromLua53
-- Compatibility notes: NovaSharp: unresolved C# interpolation placeholder
return tostring({expression})
