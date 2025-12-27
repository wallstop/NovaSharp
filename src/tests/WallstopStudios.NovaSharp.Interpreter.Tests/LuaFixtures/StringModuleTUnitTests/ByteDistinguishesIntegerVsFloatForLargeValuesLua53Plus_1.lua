-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:457
-- @test: StringModuleTUnitTests.ByteDistinguishesIntegerVsFloatForLargeValuesLua53Plus
local x = 9007199254740993.0; return string.byte('a', x)
