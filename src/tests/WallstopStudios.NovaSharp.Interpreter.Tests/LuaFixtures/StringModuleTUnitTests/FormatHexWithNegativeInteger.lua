-- @lua-versions: 5.1, 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:1410
-- @test: StringModuleTUnitTests.FormatHexWithNegativeInteger
-- Note: Lua 5.2 has stricter range checking and errors on negative values
return string.format('%x', -1)
