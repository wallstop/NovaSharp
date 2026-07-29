-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:115
-- @test: ParserTUnitTests.UnicodeEscapeSequenceAcceptedInPreLua53ModesKnownDivergence
-- Compatibility notes: Test targets Lua 5.2+
return "\u{1F40D}"
