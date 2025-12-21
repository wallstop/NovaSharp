-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:447
-- @test: Lua55SpecTUnitTests.ToNumberParsesHexadecimalStringWithoutBase
return tonumber('0xFF')
