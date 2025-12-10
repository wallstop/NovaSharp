-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:322
-- @test: Utf8ModuleTUnitTests.Utf8CharAcceptsExtendedCodePointsLua54
-- @compat-notes: Lua 5.4+ accepts code points up to 0x7FFFFFFF using extended UTF-8 encoding
return #utf8.char(0x110000)
