-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:264
-- @test: Lua55SpecTUnitTests.Utf8CharProducesUtf8String
-- @compat-notes: Test targets Lua 5.5+; Lua 5.3+: utf8 library
return utf8.char(72, 101, 108, 108, 111)
