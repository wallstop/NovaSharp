-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:95
-- @test: ParserTUnitTests.DecimalEscapeTooLargeThrowsHelpfulMessage
return "\400"
