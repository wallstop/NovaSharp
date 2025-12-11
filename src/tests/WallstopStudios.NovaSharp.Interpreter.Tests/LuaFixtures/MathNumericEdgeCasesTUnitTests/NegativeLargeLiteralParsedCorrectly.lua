-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:465
-- @test: MathNumericEdgeCasesTUnitTests.NegativeLargeLiteralParsedCorrectly
-- @compat-notes: Test targets Lua 5.4+
return -9223372036854775808
