-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:170
-- @test: ParserTUnitTests.HexFloatSourceSyntaxIsRejectedByLua51
-- Reference Lua 5.1 scans '0x0' then '.1E' and strtod rejects the fragment
-- (malformed number near '.1E'); hex floats are Lua 5.2+ source syntax.
return 0x0.1E
