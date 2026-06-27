-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:1775
-- @test: StringModuleTUnitTests.FormatHexWithMathMininteger
-- Test targets Lua 5.3+; Lua 5.3+: math.mininteger (5.3+)
return string.format('%x', math.mininteger)
