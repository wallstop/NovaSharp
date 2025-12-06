-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:265
-- @test: StringLibTUnitTests.StringMatchMatchesErrorMessage
return string.match(s, p)
