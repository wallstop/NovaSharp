-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ParserTUnitTests.cs:90
-- @test: ParserTUnitTests.DecimalEscapeTooLargeThrowsHelpfulMessage
return "\400"
