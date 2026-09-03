-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericLiteralTUnitTests.cs:361
-- @test: NumericLiteralTUnitTests.ToNumberRejectsFractionalBaseFromLua53
-- Compatibility notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.2+
return tonumber('12', {base})
