-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Tree\ParserTUnitTests.cs:70
-- @test: ParserTUnitTests.UnicodeEscapeSequenceIsDecoded
return "hi-\u{1F40D}"
