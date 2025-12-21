-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:118
-- @test: ParserTUnitTests.UnicodeEscapeSequenceAcceptedInPreLua53ModesKnownDivergence
-- @compat-notes: Test targets Lua 5.1
return "\u{1F40D}"
