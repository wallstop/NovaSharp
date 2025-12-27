-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:304
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesHexFloatWithExponent
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: hex float with exponent (5.2+)
return tonumber('0x1p2')
