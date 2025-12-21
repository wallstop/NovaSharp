-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:469
-- @test: Lua55SpecTUnitTests.ToNumberParsesHexadecimalWithBase
return tonumber('FF', 16)
