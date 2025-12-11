-- @lua-versions: 5.3
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:308
-- @test: Utf8ModuleTUnitTests.Utf8CharRejectsOutOfRangeCodePointsLua53
-- @compat-notes: Lua 5.3 rejects code points > 0x10FFFF; Lua 5.4+ accepts extended range up to 0x7FFFFFFF
return utf8.char(0x110000)
