-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs
-- @test: BasicModuleTUnitTests.ToNumberParsesHexStringWithoutBase
-- Per Lua §3.1, tonumber without base parses hex strings with 0x/0X prefix
return tonumber('0xFF')
