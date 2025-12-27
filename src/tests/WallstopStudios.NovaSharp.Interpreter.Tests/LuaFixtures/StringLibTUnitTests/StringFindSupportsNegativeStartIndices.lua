-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:77
-- @test: StringLibTUnitTests.StringFindSupportsNegativeStartIndices
return string.find('Hello Lua user', 'e', -5);
