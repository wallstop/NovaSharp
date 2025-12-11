-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaBasicMultiVersionSpecTUnitTests.cs:179
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberReturnsNilForHexStringWithInvalidChars
return tonumber('0xG')
