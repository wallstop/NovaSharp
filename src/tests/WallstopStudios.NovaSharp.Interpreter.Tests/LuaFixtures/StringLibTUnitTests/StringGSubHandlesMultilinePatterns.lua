-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:442
-- @test: StringLibTUnitTests.StringGSubHandlesMultilinePatterns
return string.gsub(a, '\
', '\
 #')
