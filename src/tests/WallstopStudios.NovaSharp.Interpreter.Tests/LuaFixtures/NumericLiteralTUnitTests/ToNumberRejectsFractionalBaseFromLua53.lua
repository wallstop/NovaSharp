-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericLiteralTUnitTests.cs:359
-- @test: NumericLiteralTUnitTests.ToNumberRejectsFractionalBaseFromLua53
-- Compatibility notes: Test targets Lua 5.2+
return tonumber('12', 3.5)
