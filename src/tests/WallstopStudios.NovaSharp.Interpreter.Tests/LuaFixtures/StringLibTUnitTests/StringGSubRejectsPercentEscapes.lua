-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:265
-- @test: StringLibTUnitTests.StringGSubRejectsPercentEscapes
-- @compat-notes: Test targets Lua 5.1
string.gsub('hello world', '%w+', '%e')
