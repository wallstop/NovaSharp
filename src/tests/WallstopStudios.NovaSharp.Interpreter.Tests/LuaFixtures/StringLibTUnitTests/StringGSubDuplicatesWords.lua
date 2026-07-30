-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:186
-- @test: StringLibTUnitTests.StringGSubDuplicatesWords
s = string.gsub('hello world', '(%w+)', '%1 %1')
                return s, s == 'hello hello world world'
