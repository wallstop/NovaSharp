-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:265
-- @test: StringLibTUnitTests.StringMatchMatchesErrorMessage
-- @compat-notes: Uses injected variable: s
return string.match(s, p)
