-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:447
-- @test: Lua55SpecTUnitTests.ToNumberParsesHexadecimalStringWithoutBase
-- @compat-notes: Test targets Lua 5.5+
return tonumber('0xFF')
