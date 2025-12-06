-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ParserTUnitTests.cs:76
-- @test: ParserTUnitTests.MalformedHexLiteralThrowsSyntaxError
return 0x1G
