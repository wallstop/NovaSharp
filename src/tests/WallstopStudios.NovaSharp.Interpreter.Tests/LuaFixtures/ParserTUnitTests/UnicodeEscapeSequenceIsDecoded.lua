-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Tree\ParserTUnitTests.cs:78
-- @test: ParserTUnitTests.UnicodeEscapeSequenceIsDecoded
-- @compat-notes: Unicode escape \u{...} syntax was introduced in Lua 5.3
return "hi-\u{1F40D}"
