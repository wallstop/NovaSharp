-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:284
-- @test: StringLibTUnitTests.StringGSubHandlesMultilinePatterns
return string.gsub(a, '\
', '\
 #')
