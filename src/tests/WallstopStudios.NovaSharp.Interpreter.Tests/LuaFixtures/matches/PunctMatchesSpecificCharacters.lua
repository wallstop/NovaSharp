-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/PatternMatching/CharacterClassParityTUnitTests.cs:630
-- @test: matches.PunctMatchesSpecificCharacters
return string.match({EscapeString(character)}, '%p') ~= nil
