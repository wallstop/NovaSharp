-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/ParserTUnitTests.cs:170
-- @test: ParserTUnitTests.HexFloatSourceSyntaxIsRejectedByLua51
-- Reference Lua 5.1 folds 'p' into the trailing-alphanumeric run and stops at the
-- signed exponent (malformed number near '0xA23p').
return 0xA23p-4
