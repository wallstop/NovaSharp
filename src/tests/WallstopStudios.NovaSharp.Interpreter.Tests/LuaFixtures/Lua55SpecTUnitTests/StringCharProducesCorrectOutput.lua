-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\Lua55SpecTUnitTests.cs:75
-- @test: Lua55SpecTUnitTests.StringCharProducesCorrectOutput
return string.char(97, 98, 99)
