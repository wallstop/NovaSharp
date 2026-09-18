-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:189
-- @test: ParserTUnitTests.MalformedNumeralsQuoteRawSourceText
-- Every reference version reports one malformed numeral quoting the raw source
-- text; the old lexer split this into 1 .. 2 (a concatenation).
return 1..2
