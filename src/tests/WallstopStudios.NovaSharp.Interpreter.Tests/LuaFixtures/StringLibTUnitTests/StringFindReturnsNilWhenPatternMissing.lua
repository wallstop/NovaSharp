-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:50
-- @test: StringLibTUnitTests.StringFindReturnsNilWhenPatternMissing
return string.find('Hello Lua user', 'banana');
