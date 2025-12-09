-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaBasicMultiVersionSpecTUnitTests.cs:105
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesLowercaseHexStringWithoutBase
return tonumber('0xff')
