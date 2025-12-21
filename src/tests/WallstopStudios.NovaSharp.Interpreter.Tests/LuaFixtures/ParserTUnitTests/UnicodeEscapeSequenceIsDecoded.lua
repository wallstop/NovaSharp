-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:78
-- @test: ParserTUnitTests.UnicodeEscapeSequenceIsDecoded
-- @compat-notes: \u{xxxx} Unicode escape sequence was introduced in Lua 5.3
return "hi-\u{1F40D}"
