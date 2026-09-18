-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:170
-- @test: ParserTUnitTests.HexFloatSourceSyntaxIsRejectedByLua51
-- Reference Lua 5.1 splits the hex-float pi constant into '0x1' plus '.921...P'
-- and strtod rejects the fragment (malformed number near '.921FB54442D18P').
return 0x1.921FB54442D18P+1
