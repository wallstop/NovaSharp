-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:129
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesNegativeHexStringWithoutBase
return tonumber('-0x10')
