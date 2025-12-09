-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:235
-- @test: Lua55SpecTUnitTests.Utf8LenReturnsCharacterCount
-- @compat-notes: Lua 5.3+: utf8 library
return utf8.len('hello')
