-- @lua-versions: 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:96
-- @test: Utf8ModuleTUnitTests.Utf8CharBuildsStringsFromCodePoints
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: utf8 library
return utf8.char(0x41, 0x1F600, 0x20AC)
