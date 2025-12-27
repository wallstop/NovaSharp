-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:927
-- @test: Utf8ModuleTUnitTests.Utf8CharEncodesValidCodePointsCorrectly
-- @compat-notes: Test targets Lua 5.5+; Lua 5.3+: utf8 library
return utf8.char(0x{codePoint:X})
