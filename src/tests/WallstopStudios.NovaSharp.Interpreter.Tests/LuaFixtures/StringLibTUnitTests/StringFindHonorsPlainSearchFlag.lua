-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:104
-- @test: StringLibTUnitTests.StringFindHonorsPlainSearchFlag
return string.find('Hello Lua user', '%su', 1, true);
