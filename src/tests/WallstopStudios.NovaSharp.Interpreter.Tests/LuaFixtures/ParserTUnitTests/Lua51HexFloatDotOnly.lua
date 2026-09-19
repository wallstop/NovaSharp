-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:170
-- @test: ParserTUnitTests.HexFloatSourceSyntaxIsRejectedByLua51
-- Reference Lua 5.1 stops the numeral scan at the dot and strtod rejects '0x'
-- (malformed number near '0x').
return 0x.8
