-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:324
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesHexFloatWithNegativeExponent
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: hex float with exponent (5.2+)
return tonumber('0x10p-2')
