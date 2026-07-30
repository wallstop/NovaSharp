-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:132
-- @test: ParserTUnitTests.MalformedHexLiteralThrowsSyntaxError
return 0x1G
