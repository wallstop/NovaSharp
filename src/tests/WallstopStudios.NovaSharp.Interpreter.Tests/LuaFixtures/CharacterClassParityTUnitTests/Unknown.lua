-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/PatternMatching/CharacterClassParityTUnitTests.cs:441
-- @test: CharacterClassParityTUnitTests.Unknown
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
return string.match(string.char({i}), '{pattern}')
