-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\Lua55SpecTUnitTests.cs:441
-- @test: Lua55SpecTUnitTests.ToNumberWithBaseParsesBinary
return tonumber('1010', 2)
