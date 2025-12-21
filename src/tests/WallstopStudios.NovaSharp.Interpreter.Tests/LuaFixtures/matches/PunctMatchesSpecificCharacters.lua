-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/PatternMatching/CharacterClassParityTUnitTests.cs:758
-- @test: matches.PunctMatchesSpecificCharacters
-- @compat-notes: Test targets Lua 5.4+
return string.match({EscapeString(character)}, '%p') ~= nil
