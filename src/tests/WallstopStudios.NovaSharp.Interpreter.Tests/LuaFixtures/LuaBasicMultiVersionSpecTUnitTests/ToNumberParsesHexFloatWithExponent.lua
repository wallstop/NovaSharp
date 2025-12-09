-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesHexFloatWithExponent
-- 0x1p2 = 1 * 2^2 = 4
-- Note: Hex float literals were introduced in Lua 5.2
return tonumber('0x1p2')
