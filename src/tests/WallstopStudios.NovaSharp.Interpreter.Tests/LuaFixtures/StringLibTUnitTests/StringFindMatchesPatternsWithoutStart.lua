-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:86
-- @test: StringLibTUnitTests.StringFindMatchesPatternsWithoutStart
return string.find('Hello Lua user', '%su');
