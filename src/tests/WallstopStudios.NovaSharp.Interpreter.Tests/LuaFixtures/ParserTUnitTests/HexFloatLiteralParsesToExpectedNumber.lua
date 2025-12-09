-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:60
-- @test: ParserTUnitTests.HexFloatLiteralParsesToExpectedNumber
-- @compat-notes: Lua 5.2+: hex float literal (5.2+)
return 0x1.fp3
