-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs
-- @test: BasicModuleTUnitTests.ToNumberParsesHexFloatWithFraction
-- 0x1.8 = 1 + 8/16 = 1.5, p0 means * 2^0 = 1.5
-- Note: Hex float literals were introduced in Lua 5.2
return tonumber('0x1.8p0')
