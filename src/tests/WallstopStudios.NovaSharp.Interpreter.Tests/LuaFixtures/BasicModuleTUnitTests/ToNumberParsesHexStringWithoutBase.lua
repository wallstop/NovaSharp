-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs
-- @test: BasicModuleTUnitTests.ToNumberParsesHexStringWithoutBase
-- Per Lua §3.1, tonumber without base parses hex strings with 0x/0X prefix
-- Note: Hex string parsing in tonumber without explicit base was added in Lua 5.2
return tonumber('0xFF')
