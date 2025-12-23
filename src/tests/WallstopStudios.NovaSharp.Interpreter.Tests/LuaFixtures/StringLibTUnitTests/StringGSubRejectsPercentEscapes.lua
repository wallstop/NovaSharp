-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:268
-- @test: StringLibTUnitTests.StringGSubRejectsPercentEscapes
string.gsub('hello world', '%w+', '%e')
