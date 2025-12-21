-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:286
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesHexFloatWithFraction
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: hex float literal (5.2+)
return tonumber('0x1.8p0')
