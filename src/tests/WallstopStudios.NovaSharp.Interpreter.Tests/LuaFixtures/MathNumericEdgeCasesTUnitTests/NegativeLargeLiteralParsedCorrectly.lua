-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:563
-- @test: MathNumericEdgeCasesTUnitTests.NegativeLargeLiteralParsedCorrectly
-- @compat-notes: Test targets Lua 5.3+
return -9223372036854775808
