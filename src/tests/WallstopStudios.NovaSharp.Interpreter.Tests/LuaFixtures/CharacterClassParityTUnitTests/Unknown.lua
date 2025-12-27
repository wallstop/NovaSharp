-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/PatternMatching/CharacterClassParityTUnitTests.cs:442
-- @test: CharacterClassParityTUnitTests.Unknown
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return string.match(string.char({i}), '{pattern}')
