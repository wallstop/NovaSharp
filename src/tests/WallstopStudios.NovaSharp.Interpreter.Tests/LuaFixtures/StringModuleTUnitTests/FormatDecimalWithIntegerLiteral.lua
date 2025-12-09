-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:1390
-- @test: StringModuleTUnitTests.FormatDecimalWithIntegerLiteral
return string.format('%d', 9223372036854775807)
