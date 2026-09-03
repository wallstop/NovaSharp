-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericLiteralTUnitTests.cs:347
-- @test: NumericLiteralTUnitTests.ToNumberTruncatesFractionalBaseBeforeLua53
-- Compatibility notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.2+
return tonumber('12', {base})
