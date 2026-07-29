-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Tree\ParserTUnitTests.cs:67
-- @test: ParserTUnitTests.HexFloatLiteralParsesToExpectedNumber
-- Lua 5.2+: hex float literal (5.2+)
return 0x1.fp3
