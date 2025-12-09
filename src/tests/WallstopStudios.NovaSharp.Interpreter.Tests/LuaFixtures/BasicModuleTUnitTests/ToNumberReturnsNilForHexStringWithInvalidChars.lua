-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs
-- @test: BasicModuleTUnitTests.ToNumberReturnsNilForHexStringWithInvalidChars
-- "0xG" contains invalid hex digit, should return nil
return tonumber('0xG')
