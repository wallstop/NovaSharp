-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:274
-- @test: Lua55SpecTUnitTests.Utf8CodepointReturnsCodePoints
-- @compat-notes: Test targets Lua 5.5+; Lua 5.3+: utf8 library
return utf8.codepoint('ABC', 1, 3)
