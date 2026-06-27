-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\Lua55SpecTUnitTests.cs:469
-- @test: Lua55SpecTUnitTests.ToNumberParsesHexadecimalWithBase
-- Test targets Lua 5.5+
return tonumber('FF', 16)
