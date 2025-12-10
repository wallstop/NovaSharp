-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:1400
-- @test: StringModuleTUnitTests.FormatDecimalWithFloatFallsBackToConversion
-- @compat-notes: Lua 5.1/5.2 truncate float to integer for %d; Lua 5.3+ requires integer representation
return string.format('%d', 123.456)
