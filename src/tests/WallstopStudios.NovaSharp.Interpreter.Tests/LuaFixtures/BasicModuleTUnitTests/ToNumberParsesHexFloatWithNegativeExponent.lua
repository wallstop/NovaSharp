-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs
-- @test: BasicModuleTUnitTests.ToNumberParsesHexFloatWithNegativeExponent
-- 0x10p-2 = 16 * 2^(-2) = 16 / 4 = 4
-- Note: Hex float literals were introduced in Lua 5.2
return tonumber('0x10p-2')
