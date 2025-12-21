-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:95
-- @test: StringLibTUnitTests.StringFindMatchesPatternsWithStart
return string.find('Hello Lua user', '%su', 1);
