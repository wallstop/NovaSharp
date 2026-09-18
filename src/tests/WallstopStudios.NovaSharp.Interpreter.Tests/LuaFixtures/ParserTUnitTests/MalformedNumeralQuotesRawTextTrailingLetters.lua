-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:189
-- @test: ParserTUnitTests.MalformedNumeralsQuoteRawSourceText
-- Trailing alphanumerics fold into the numeral and full-string consumption
-- rejects them (malformed number near '123abc').
return 123abc
