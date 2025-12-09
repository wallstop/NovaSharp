-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs
-- @test: BasicModuleTUnitTests.ToNumberParsesNegativeHexStringWithoutBase
-- Note: Hex string parsing in tonumber without explicit base was added in Lua 5.2
return tonumber('-0x10')
