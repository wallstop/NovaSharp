-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:243
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesHexFloatWithPositiveExponentSign
-- @compat-notes: Lua 5.2+: hex float with exponent (5.2+)
return tonumber('0x1p+2')
