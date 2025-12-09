-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:204
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesHexFloatWithFraction
-- @compat-notes: Lua 5.2+: hex float literal (5.2+)
return tonumber('0x1.8p0')
