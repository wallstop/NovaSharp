-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:93
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesHexStringWithoutBase
return tonumber('0xFF')
