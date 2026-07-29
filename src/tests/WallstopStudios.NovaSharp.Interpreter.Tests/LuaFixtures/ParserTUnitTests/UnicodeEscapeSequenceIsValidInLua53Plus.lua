-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:94
-- @test: ParserTUnitTests.UnicodeEscapeSequenceIsValidInLua53Plus
-- Compatibility notes: Test targets Lua 5.3+
return "\u{1F40D}"
