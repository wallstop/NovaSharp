-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:303
-- @test: StringLibTUnitTests.StringGSubHandlesMultilinePatterns
return string.gsub(a, '\
', '\
 #')
