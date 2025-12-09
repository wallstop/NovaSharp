-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberReturnsNilForHexStringWithInvalidChars
-- "0xG" contains invalid hex digit
return tonumber('0xG')
