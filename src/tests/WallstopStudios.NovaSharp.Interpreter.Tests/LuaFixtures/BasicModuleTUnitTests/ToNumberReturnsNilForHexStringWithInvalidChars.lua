-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs
-- @test: BasicModuleTUnitTests.ToNumberReturnsNilForHexStringWithInvalidChars
-- "0xG" contains invalid hex digit, should return nil
-- Note: Hex parsing without explicit base was added in Lua 5.2; in Lua 5.1 all hex strings return nil
return tonumber('0xG')
