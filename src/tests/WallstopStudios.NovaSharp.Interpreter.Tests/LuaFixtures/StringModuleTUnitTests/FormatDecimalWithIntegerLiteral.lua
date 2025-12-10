-- @lua-versions: 5.1, 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:1390
-- @test: StringModuleTUnitTests.FormatDecimalWithIntegerLiteral
-- Note: Lua 5.2 has stricter range checking and errors on large integers
return string.format('%d', 9223372036854775807)
