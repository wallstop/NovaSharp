-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:254
-- @test: Lua55SpecTUnitTests.Utf8LenReturnsCharacterCount
-- @compat-notes: Test targets Lua 5.5+; Lua 5.3+: utf8 library
return utf8.len('hello')
