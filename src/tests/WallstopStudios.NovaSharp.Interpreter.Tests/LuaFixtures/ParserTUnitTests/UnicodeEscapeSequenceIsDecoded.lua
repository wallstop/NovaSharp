-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ParserTUnitTests.cs:65
-- @test: ParserTUnitTests.UnicodeEscapeSequenceIsDecoded
return "hi-\u{1F40D}"
