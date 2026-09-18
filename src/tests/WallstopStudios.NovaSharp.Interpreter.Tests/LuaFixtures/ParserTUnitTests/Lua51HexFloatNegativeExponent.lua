-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:170
-- @test: ParserTUnitTests.HexFloatSourceSyntaxIsRejectedByLua51
-- Reference Lua 5.1 stops the scan at the signed p-exponent (malformed number
-- near '0x8p'); Lua 5.2+ evaluate this as 1.0.
return 0x8p-3
