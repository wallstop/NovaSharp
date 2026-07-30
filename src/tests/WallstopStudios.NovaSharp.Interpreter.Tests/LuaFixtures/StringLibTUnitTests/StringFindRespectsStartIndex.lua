-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:59
-- @test: StringLibTUnitTests.StringFindRespectsStartIndex
return string.find('Hello Lua user', 'Lua', 1);
